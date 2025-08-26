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

private:
    FString GetLogPath() const
    {
        // Use a more accessible location - save in the server directory
        return TEXT("G:\\ToS\\Server\\client_debug.log");
    }

public:

        void ClearLog()
    {
#if !UE_BUILD_SHIPPING
        FScopeLock Lock(&LogMutex);
        const FString ClientLogPath = GetLogPath();
        FPlatformFileManager::Get().GetPlatformFile().DeleteFile(*ClientLogPath);

        // Log the file location to UE console
        UE_LOG(LogTemp, Warning, TEXT("[LOG] Client debug log: %s"), *ClientLogPath);

                        // Add session header with proper timestamp
        FDateTime Now = FDateTime::Now();
        FString SessionHeader = FString::Printf(TEXT("=== CLIENT SESSION STARTED: %04d-%02d-%02d %02d:%02d:%02d ===\r\n"),
            Now.GetYear(), Now.GetMonth(), Now.GetDay(), Now.GetHour(), Now.GetMinute(), Now.GetSecond());
        FFileHelper::SaveStringToFile(SessionHeader, *ClientLogPath, FFileHelper::EEncodingOptions::ForceUTF8WithoutBOM, &IFileManager::Get(), FILEWRITE_Append);
#endif
    }

                    void Log(const FString& Message)
    {
        FScopeLock Lock(&LogMutex);

        const FString ClientLogPath = GetLogPath();
        FDateTime Now = FDateTime::Now();
        FString Timestamp = FString::Printf(TEXT("%02d:%02d:%02d.%03d"),
            Now.GetHour(), Now.GetMinute(), Now.GetSecond(), Now.GetMillisecond());

        // Convert emojis to text equivalents to avoid binary corruption
        FString CleanMessage = Message;
        CleanMessage = CleanMessage.Replace(TEXT("🎯"), TEXT("[TARGET]"));
        CleanMessage = CleanMessage.Replace(TEXT("🚀"), TEXT("[ROCKET]"));
        CleanMessage = CleanMessage.Replace(TEXT("❌"), TEXT("[X]"));
        CleanMessage = CleanMessage.Replace(TEXT("📡"), TEXT("[SATELLITE]"));
        CleanMessage = CleanMessage.Replace(TEXT("📤"), TEXT("[OUTBOX]"));
        CleanMessage = CleanMessage.Replace(TEXT("✅"), TEXT("[CHECK]"));
        CleanMessage = CleanMessage.Replace(TEXT("🔧"), TEXT("[WRENCH]"));
        CleanMessage = CleanMessage.Replace(TEXT("🌐"), TEXT("[GLOBE]"));
        CleanMessage = CleanMessage.Replace(TEXT("📦"), TEXT("[PACKAGE]"));
        CleanMessage = CleanMessage.Replace(TEXT("🔍"), TEXT("[MAGNIFYING]"));

        FString LogLine = FString::Printf(TEXT("[%s] %s\r\n"), *Timestamp, *CleanMessage);

        FFileHelper::SaveStringToFile(LogLine, *ClientLogPath, FFileHelper::EEncodingOptions::ForceUTF8WithoutBOM, &IFileManager::Get(), FILEWRITE_Append);
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
#if !UE_BUILD_SHIPPING
    FFileLogger::Get().Log(Message);
#endif
}

void ClientFileLogHex(const FString& Prefix, const TArray<uint8>& Data)
{
#if !UE_BUILD_SHIPPING
    FFileLogger::Get().LogHex(Prefix, Data);
#endif
}
