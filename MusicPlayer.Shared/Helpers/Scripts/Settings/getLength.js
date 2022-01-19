let storage = new window.Sifrr.Storage("$StorageOptions");

var keys = await storage.keys();
return keys.length.toString();