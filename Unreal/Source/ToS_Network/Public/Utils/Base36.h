#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "Base36.generated.h"

UCLASS()
class TOS_NETWORK_API UBase36 : public UBlueprintFunctionLibrary
{
    GENERATED_BODY()

public:

    UFUNCTION(BlueprintCallable, Category = "Base36")
    static int32 Base36ToInt(const FString& Base36String);

    UFUNCTION(BlueprintCallable, Category = "Base36")
    static FString IntToBase36(int32 Value);
};
