let storage = new window.Sifrr.Storage("$StorageOptions");

var data = await storage.get("$Key");

var key = Object.keys(data)[0];
var value = data[key];

if (value == undefined) return "null";
else return value;