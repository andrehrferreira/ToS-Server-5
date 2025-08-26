#pragma once

#include "CoreMinimal.h"

// Global functions for logging to file
void ClientFileLog(const FString& Message);
void ClientFileLogHex(const FString& Prefix, const TArray<uint8>& Data);
