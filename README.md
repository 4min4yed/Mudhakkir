# Mudhakkir
### Don't let your work keep you away from the remembrance of ALLAH SWT
### لا تدع عملك يشغلك عن ذكر الله سبحانه و تعالى

#### a custom timed reminder app for any Dhikr you want in two different notification modes

##### Download the .exe directly:
[Here](https://mohamedaminayedportfolio.netlify.app/assets/Mudhakkir.rar)

##### Or built it yourself:
Required:
Install SDK from:
[.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-8.0.426-windows-x64-installer)

Do these in the same folder as the project files: \
For test use, simply run: \
`dotnet build` \
`dotnet run` 

To export into a standalone *.exe*: \
`dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishReadyToRun=true`
