# Headless

The purpose of the application is to start the other console application on hidden desktop.

## Running application

### Supported options

Options are specified in one of the following forms:

* `--<option name> <option value>`
* `--<option name>=<option value>`

Available options:

* `--driver <driver path>` - specifies driver executable name. Tool till try to locate with PATH, so you could omit the path. Default is `chromedriver.exe`
* `--desktop <desktop name>` - specifies the name of hidden desktop. In case it's not specified the random name will be used.
* `--desktop=false` - instructs not to create hidden desktop. Driver will run on user desktop.

### Passing parameters down to the driver

Simply use `--` in command line to tell `headless` that subsequent parameters should be applied to driver executable.

```bash
headless.exe --driver=c:\chromedriver.exe
headless.exe --driver c:\chromedriver.exe -- -port=12345
headless.exe --driver c:\chromedriver.exe --desktop headless-mine

headless.exe --driver c:\chromedriver.exe --desktop=false
```
