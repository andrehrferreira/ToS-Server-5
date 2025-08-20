#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "ChaCha20Poly1305.generated.h"

UCLASS()
class TOS_NETWORK_API UChaCha20Poly1305 : public UBlueprintFunctionLibrary
{
    GENERATED_BODY()
public:
    UFUNCTION(BlueprintCallable, Category = "Crypto")
    static bool ChaCha20Poly1305Ietf_Encrypt(const TArray<uint8>& Key, const TArray<uint8>& Nonce, const TArray<uint8>& AAD, const TArray<uint8>& Plain, TArray<uint8>& Cipher);

    UFUNCTION(BlueprintCallable, Category = "Crypto")
    static bool ChaCha20Poly1305Ietf_Decrypt(const TArray<uint8>& Key, const TArray<uint8>& Nonce, const TArray<uint8>& AAD, const TArray<uint8>& Cipher, TArray<uint8>& Plain);
};
