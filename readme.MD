## Running application


```bash
headless.exe c:\chromedriver.exe
headless.exe --app=c:\chromedriver.exe
headless.exe --app c:\chromedriver.exe
headless.exe --app c:\chromedriver.exe --desktop headless-mine
set HEADLESS_APP=c:\chromedriver.exe && headless.exe

headless.exe c:\chromedriver.exe --desktop=false
` example of passing parameters to callee application`
headless.exe c:\chromedriver.exe -- --port=9126
```
