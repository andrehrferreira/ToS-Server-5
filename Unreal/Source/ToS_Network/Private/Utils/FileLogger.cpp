#include "Utils/FileLogger.h"
#include "CoreMinimal.h"
#include "HAL/PlatformFilemanager.h"
#include "Misc/FileHelper.h"
#include "Misc/DateTime.h"

class FFileLogger
{
public:
    static FFileLogger& Get()
    {
        static FFileLogger Instance;
        return Instance;
    }

    void ClearLog()
    {
        FScopeLock Lock(&LogMutex);
        const FString ClientLogPath = FPaths::ProjectDir() + TEXT("client_debug.log");
        FPlatformFileManager::Get().GetPlatformFile().DeleteFile(*ClientLogPath);
    }

    void Log(const FString& Message)
    {
        FScopeLock Lock(&LogMutex);

        const FString ClientLogPath = FPaths::ProjectDir() + TEXT("client_debug.log");
        FString Timestamp = FDateTime::Now().ToString(TEXT("HH:mm:ss.fff"));
        FString LogLine = FString::Printf(TEXT("[%s] %s\n"), *Timestamp, *Message);

        FFileHelper::SaveStringToFile(LogLine, *ClientLogPath, FFileHelper::EEncodingOptions::AutoDetect, &IFileManager::Get(), FILEWRITE_Append);
    }

    void LogHex(const FString& Prefix, const TArray<uint8>& Data)
    {
        if (Data.Num() == 0) return;

        FString Hex;
        for (int32 i = 0; i < Data.Num(); i++)
        {
            Hex += FString::Printf(TEXT("%02X"), Data[i]);
        }
        Log(FString::Printf(TEXT("%s: %s"), *Prefix, *Hex));
    }

private:
    FFileLogger()
    {
        ClearLog(); // Clear log on startup
    }

    FCriticalSection LogMutex;
};

// Global functions for easy access
void ClientFileLog(const FString& Message)
{
    FFileLogger::Get().Log(Message);
}

void ClientFileLogHex(const FString& Prefix, const TArray<uint8>& Data)
{
    FFileLogger::Get().LogHex(Prefix, Data);
}
