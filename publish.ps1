if(Test-Path .\bin)
{
    Remove-Item -Path .\bin -Recurse
}

dotnet publish .\YouNewThis -c release -o bin\younewthis -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true -r win-x64 --self-contained
dotnet publish .\YouNewThat -c release -o bin\younewthat -p:PublishSingleFile=true -p:PublishTrimmed=true -r linux-x64 --self-contained