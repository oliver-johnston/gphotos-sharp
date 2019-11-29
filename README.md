# gphotos-sharp
CLI for uploading photos to Google Photos

[![Build Status](https://travis-ci.com/oliver-johnston/gphotos-sharp.svg?branch=master)](https://travis-ci.com/oliver-johnston/gphotos-sharp)

# Building

```
git clone https://github.com/oliver-johnston/gphotos-sharp.git
cd gphotos-sharp/GooglePhotosSharp
dotnet build
```

# Config

The program requires a directory containing two files:
 - _client_secret.json_: this is the API key obtained from Google. 
 - _config.json_: this defines the locations of your libraries and which accounts to upload them to. 

```
[
  {
    "path": "/path/to/library", 
    "email": "youraccount@gmail.com",
    "includeRegexes": [".jpg"] // optional
    "excludeRegexes": [".bmp"] // optional
  }, 
  // more libraries here
]
```

# Running
`dotnet GooglePhotosSharp.dll --configFolder /path/to/config`
