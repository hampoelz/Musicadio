using Windows.UI.Xaml.Controls;
using Uno.UI.Runtime.WebAssembly;
using Windows.UI.Xaml;
using System.Collections.Generic;
using Uno.Foundation;
using System.Linq;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Musicadio.Shared.Html.Api.Components
{
    // TODO: Complete rewrite 

    [HtmlElement("input")]
    public class FilePicker : Control, IFilePicker
    {
        // TODO: Add NativeFS functionality
        // https://developer.mozilla.org/de/docs/Web/HTML/Element/input/file
        // https://developer.mozilla.org/en-US/docs/Web/API/File/Using_files_from_web_applications
        // https://developer.mozilla.org/en-US/docs/Web/API/File_and_Directory_Entries_API

        // https://web.dev/file-system-access/

        public FilePicker()
        {
            this.SetHtmlAttribute("type", "file");
            this.ExecuteJavascript($"element.style.display = 'none';");

            //TODO: Add Error handler
            this.ExecuteJavascript(@"element.showOpenFilePickerFallback = () =>
                new Promise((resolve, reject) => {
                    element.addEventListener('change', () => resolve(element.files), { once: true });
                    element.click();
                });");
        }

        public class File
        {
            public string Name { get; }
            public string DisplayName { get; }
            public string FileType { get; }
            public string ContentType { get; }
            public string RelativePath { get; }
            public string Path { get; }
            public Uri ObjectUrl { get; }
            public ulong Size { get; }
            public DateTimeOffset DateModified { get; }

            public File(FilePicker filePicker, int file)
            {
                if (filePicker.ExecuteJavascript($"element.pickedFiles[{file}]") == "undefined")
                    throw new InvalidCastException("Failed to parse File-Id");

                this.Name = filePicker.ExecuteJavascript($"element.pickedFiles[{file}].name");
                this.DisplayName = System.IO.Path.GetFileNameWithoutExtension(this.Name);
                this.FileType = System.IO.Path.GetExtension(this.Name).Replace(".", "");

                var contentType = filePicker.ExecuteJavascript($"element.pickedFiles[{file}].type");
                this.ContentType = contentType == "undefined" ? null : contentType;

                var relativePath = filePicker.ExecuteJavascript($"element.pickedFiles[{file}].webkitRelativePath");
                this.RelativePath = relativePath == "undefined" ? null : relativePath;

                var path = filePicker.ExecuteJavascript($"element.pickedFiles[{file}].path");
                this.Path = path == "undefined" ? null : path;

                var objectUrl = filePicker.ExecuteJavascript($"URL.createObjectURL(element.pickedFiles[{file}]);");
                this.ObjectUrl = new Uri(objectUrl);

                var size = filePicker.ExecuteJavascript($"element.pickedFiles[{file}].size");
                this.Size = ulong.Parse(size);

                var dateModified = filePicker.ExecuteJavascript($"element.pickedFiles[{file}].lastModifiedDate.toJSON();");
                this.DateModified = DateTimeOffset.Parse(dateModified, CultureInfo.InvariantCulture);
            }

            public void Revoke()
            {
                WebAssemblyRuntime.InvokeJS($"URL.revokeObjectURL({this.ObjectUrl});");

                var props = this.GetType().GetProperties();
                foreach (var prop in props) prop.SetValue(this, null);
            }
        }

        public class StorageFile
        {
            
        }

        public async Task<File[]> PickAsync()
        {
            this.ExecuteJavascript("element.fileHandle = undefined;");
            this.ExecuteJavascript("element.pickedFiles = [];");

            if (App.UsePwaWrapper || !hasNativeFS) await this.ExecuteJavascriptAsync("element.pickedFiles = await element.showOpenFilePickerFallback();");
            else
            {
                FileTypeFilterFormat jsonFormat;

                if (hasOpenFilePickerAPI) jsonFormat = FileTypeFilterFormat.OpenFilePickerAPI;
                else if (hasFileSystemEntriesAPI) jsonFormat = FileTypeFilterFormat.OpenFilePickerAPI;
                else return null;

                var fileTypeFilter = GetFileTypeFilterJson(jsonFormat);
                var options = $"{{ multiple: {PickMultipleFiles.ToString().ToLower()}, {fileTypeFilter} }}";

                if (hasOpenFilePickerAPI) await this.ExecuteJavascriptAsync($"element.fileHandle = await window.showOpenFilePicker({options});");
                if (hasFileSystemEntriesAPI) await this.ExecuteJavascriptAsync($"element.fileHandle = await window.chooseFileSystemEntries({options});");

                await this.ExecuteJavascriptAsync("for (const fileHandle of element.fileHandle) element.pickedFiles.push(await fileHandle.getFile());");
            }

            var filesCount = int.Parse(this.ExecuteJavascript("element.pickedFiles.length"));
            var fileIDs = Enumerable.Range(0, filesCount).ToArray();

            Files = fileIDs.Select(fileID => new File(this, fileID)).ToArray();

            return Files;
        }

        public async Task<bool> VerifyReadPermission(File file)
        {
            //if ()
            var queryPermission = await this.ExecuteJavascriptAsync("await fileHandle.queryPermission(opts)");
            if (queryPermission == "granted") return true;

            var requestPermission = await this.ExecuteJavascriptAsync("await fileHandle.requestPermission(opts)");
            if (requestPermission == "granted") return true;

            return false;
        }

        #region Properties

        // Changes between FileSystemEntriesAPI & OpenFilePickerAPI: https://github.com/WICG/file-system-access/blob/master/changes.md
        public static readonly bool hasNativeFS = hasFileSystemEntriesAPI || hasOpenFilePickerAPI;
        public static readonly bool hasFileSystemEntriesAPI = bool.Parse(WebAssemblyRuntime.InvokeJS("'chooseFileSystemEntries' in window"));
        public static readonly bool hasOpenFilePickerAPI = bool.Parse(WebAssemblyRuntime.InvokeJS("'showOpenFilePicker' in window"));
        
        public File[] Files { get; private set; }

        #region FileTypeFilterProperty

        public static readonly DependencyProperty FileTypeFilterProperty = DependencyProperty.Register(
            "FileTypeFilter",
            typeof(List<(string, List<(string, List<string>)>)>),
            typeof(FilePicker),
            new PropertyMetadata(default(List<(string, List<(string, List<string>)>)>), FileTypeFilterChanged));

        public List<(string FileTypeDescription, List<(string MimeType, List<string> FileExtensions)> FileTypes)> FileTypeFilter
        {
            get => (List<(string, List<(string, List<string>)>)>)GetValue(FileTypeFilterProperty);
            set => SetValue(FileTypeFilterProperty, value);
        }

        public enum FileTypeFilterFormat
        {
            Fallback,
            FileSystemEntriesAPI,
            OpenFilePickerAPI
        }

        public string GetFileTypeFilterJson(FileTypeFilterFormat format)
        {
            switch (format)
            {
                // For Chrome 85 and earlier:
                case FileTypeFilterFormat.FileSystemEntriesAPI:
                    {
                        // JSON Format:
                        /* types: [
                                {
                                    description: 'Text Files',
                                    accept: {
                                        'text/plain': ['.txt', '.text'],
                                        'text/html': ['.html', '.htm']
                                    }
                                },
                                {
                                    description: 'Images',
                                    accept: {
                                        'image/*': ['.png', '.gif', '.jpeg', '.jpg']
                                    }
                                }
                        ] */

                        var json = "types: [";
                        foreach ((string fileTypeDescription, var fileTypes) in FileTypeFilter)
                        {
                            json += "{";
                            if (!string.IsNullOrEmpty(fileTypeDescription)) json += $"description: '{fileTypeDescription}', ";

                            json += "accept: {";
                            foreach ((string mimeType, List<string> fileExtensions) in fileTypes)
                            {
                                json += $"'{mimeType}': ['{string.Join("', '", fileExtensions)}']";
                                json += (mimeType, fileExtensions) != fileTypes[fileTypes.Count - 1] ? ", " : "";
                            }
                            json += "}";

                            json += "}" + ((fileTypeDescription, fileTypes) != FileTypeFilter[FileTypeFilter.Count - 1] ? ", " : "");
                        }
                        json += "]";

                        return json;
                    }

                // For Chrome 86 and later:             
                case FileTypeFilterFormat.OpenFilePickerAPI:
                    {
                        // JSON Format:
                        /* accepts: [
                            {
                                description: 'Text Files',
                                mimeTypes: ['text/plain', 'text/html'],
                                extensions: ['txt', 'text', 'html', 'htm']
                            },
                            {
                                description: 'Images',
                                mimeTypes: ['image/*'],
                                extensions: ['png', 'gif', 'jpeg', 'jpg']
                            }
                        ] */

                        var json = "accepts: [";
                        foreach ((string fileTypeDescription, List<(string mimeType, List<string> fileExtensions)> fileTypes) in FileTypeFilter)
                        {
                            json += "{";
                            if (!string.IsNullOrEmpty(fileTypeDescription)) json += $"description: '{fileTypeDescription}', ";

                            var mimeTypeList = fileTypes.Select(fileType => fileType.mimeType).ToArray();
                            var fileExtensionList = fileTypes.SelectMany(fileType => fileType.fileExtensions).ToArray();

                            json += $"mimeTypes: ['{string.Join("', '", mimeTypeList)}'], ";
                            json += $"extensions: ['{string.Join("', '", fileExtensionList)}']";

                            json += "}" + ((fileTypeDescription, fileTypes) != FileTypeFilter[FileTypeFilter.Count - 1] ? ", " : "");
                        }
                        json += "]";

                        return json;
                    }

                // For browsers that don't support Native FS:
                case FileTypeFilterFormat.Fallback:
                default:
                    var extensions = FileTypeFilter.SelectMany(fileTypeFilter => fileTypeFilter.FileTypes.SelectMany(fileTypes => fileTypes.FileExtensions.Select(fileExtension => fileExtension))).ToArray();
                    return string.Join(", ", extensions);
            }
        }

        private static void FileTypeFilterChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
        {
            var element = dependencyobject as FilePicker;

            var fileTypeFilter = element.GetFileTypeFilterJson(FileTypeFilterFormat.Fallback);
            element?.SetHtmlAttribute("accept", fileTypeFilter);
        }

        #endregion

        #region PickMultipleFilesProperty

        public static readonly DependencyProperty PickMultipleFilesProperty = DependencyProperty.Register(
            "PickMultipleFiles",
            typeof(bool),
            typeof(FilePicker),
            new PropertyMetadata(default(bool), PickMultipleFilesChanged));

        public bool PickMultipleFiles
        {
            get => (bool)GetValue(PickMultipleFilesProperty);
            set => SetValue(PickMultipleFilesProperty, value);
        }

        private static void PickMultipleFilesChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
        {
            var element = dependencyobject as FilePicker;
            var multiple = ((bool)args.NewValue).ToString().ToLower();
            element?.SetHtmlAttribute("multiple", multiple);
        }

        #endregion

        // Stream object:
        /*
            - Uri
            - IsLiveStream
            ...
        */

        // AudioFile object:
        /*
            - UUID in Database
            - Title
            - Artist / Artists
                - Additional infos from MusicBrainz (Online)
            - Album
                - Additional infos from MusicBrainz (Online)
            - Album Artist
                - Additional infos from MusicBrainz (Online)
            - Track
            - Beats per minute
            - Genre
            - Label ?
            - Cover Image
            - Yeah
            - Duration
            - Size
            - isMono / Number of Channels
            - Warnings ?
            - Codec
            - Codec Profile
            - Container
            - Bitrate
            - SampleRate
            - Lyrics (Online)
            - Add additional/missing infos from MusicBrainz (Online)
        */

        // Playlist object:
        /*
            - List of AudioFile objects
        */

        #endregion

        #region Events

        #endregion
    }
}