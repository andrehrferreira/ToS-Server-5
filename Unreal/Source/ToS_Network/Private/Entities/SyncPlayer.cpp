#include "Entities/SyncPlayer.h"
#include "Engine/LocalPlayer.h"
#include "Utils/Base36.h"
#include "Utils/CRC32C.h"
#include "Camera/CameraComponent.h"
#include "Components/CapsuleComponent.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "GameFramework/SpringArmComponent.h"
#include "GameFramework/Controller.h"
#include "EnhancedInputComponent.h"
#include "EnhancedInputSubsystems.h"
#include "InputActionValue.h"
#include "Utils/FileLogger.h"

ASyncPlayer::ASyncPlayer()
{
    LocalControl = true;

    GetCapsuleComponent()->InitCapsuleSize(42.f, 96.0f);

    bUseControllerRotationPitch = false;
    bUseControllerRotationYaw = false;
    bUseControllerRotationRoll = false;

    GetCharacterMovement()->bOrientRotationToMovement = true;
    GetCharacterMovement()->RotationRate = FRotator(0.0f, 500.0f, 0.0f);

    GetCharacterMovement()->JumpZVelocity = 700.f;
    GetCharacterMovement()->AirControl = 0.35f;
    GetCharacterMovement()->MaxWalkSpeed = 500.f;
    GetCharacterMovement()->MinAnalogWalkSpeed = 20.f;
    GetCharacterMovement()->BrakingDecelerationWalking = 2000.f;
    GetCharacterMovement()->BrakingDecelerationFalling = 1500.0f;

    CameraBoom = CreateDefaultSubobject<USpringArmComponent>(TEXT("CameraBoom"));
    CameraBoom->SetupAttachment(RootComponent);
    CameraBoom->TargetArmLength = 400.0f;
    CameraBoom->bUsePawnControlRotation = true;

    FollowCamera = CreateDefaultSubobject<UCameraComponent>(TEXT("FollowCamera"));
    FollowCamera->SetupAttachment(CameraBoom, USpringArmComponent::SocketName);
    FollowCamera->bUsePawnControlRotation = false;
}

void ASyncPlayer::BeginPlay()
{
    Super::BeginPlay();

            UE_LOG(LogTemp, Warning, TEXT("🎯 SyncPlayer::BeginPlay - NetSubsystem: %s"), NetSubsystem ? TEXT("FOUND") : TEXT("NULL"));
    ClientFileLog(FString::Printf(TEXT("🎯 SyncPlayer::BeginPlay - NetSubsystem: %s"), NetSubsystem ? TEXT("FOUND") : TEXT("NULL")));

    if (NetSubsystem)
    {
        GetWorld()->GetTimerManager().SetTimer(NetSyncTimerHandle, this, &ASyncPlayer::SendSyncToServer, 0.1f, true);
        UE_LOG(LogTemp, Warning, TEXT("🚀 SyncPlayer: Timer started for SendSyncToServer every 0.1s"));
        ClientFileLog(TEXT("🚀 SyncPlayer: Timer started for SendSyncToServer every 0.1s"));
    }
    else
    {
        UE_LOG(LogTemp, Error, TEXT("❌ SyncPlayer: NetSubsystem is NULL! No sync packets will be sent!"));
        ClientFileLog(TEXT("❌ SyncPlayer: NetSubsystem is NULL! No sync packets will be sent!"));
    }
}

void ASyncPlayer::NotifyControllerChanged()
{
    Super::NotifyControllerChanged();

    if (APlayerController* PlayerController = Cast<APlayerController>(Controller))
    {
        if (UEnhancedInputLocalPlayerSubsystem* Subsystem = ULocalPlayer::GetSubsystem<UEnhancedInputLocalPlayerSubsystem>(PlayerController->GetLocalPlayer()))
        {
            Subsystem->AddMappingContext(DefaultMappingContext, 0);
        }
    }
}

void ASyncPlayer::SetupPlayerInputComponent(UInputComponent* PlayerInputComponent)
{
    if (UEnhancedInputComponent* EnhancedInputComponent = Cast<UEnhancedInputComponent>(PlayerInputComponent)) {
        EnhancedInputComponent->BindAction(JumpAction, ETriggerEvent::Started, this, &ACharacter::Jump);
        EnhancedInputComponent->BindAction(JumpAction, ETriggerEvent::Completed, this, &ACharacter::StopJumping);
        EnhancedInputComponent->BindAction(MoveAction, ETriggerEvent::Triggered, this, &ASyncPlayer::Move);
        EnhancedInputComponent->BindAction(LookAction, ETriggerEvent::Triggered, this, &ASyncPlayer::Look);
    }
}

void ASyncPlayer::Move(const FInputActionValue& Value)
{
    FVector2D MovementVector = Value.Get<FVector2D>();

    if (Controller != nullptr)
    {
        const FRotator Rotation = Controller->GetControlRotation();
        const FRotator YawRotation(0, Rotation.Yaw, 0);

        const FVector ForwardDirection = FRotationMatrix(YawRotation).GetUnitAxis(EAxis::X);
        const FVector RightDirection = FRotationMatrix(YawRotation).GetUnitAxis(EAxis::Y);

        AddMovementInput(ForwardDirection, MovementVector.Y);
        AddMovementInput(RightDirection, MovementVector.X);
    }
}

void ASyncPlayer::Look(const FInputActionValue& Value)
{
    FVector2D LookAxisVector = Value.Get<FVector2D>();

    if (Controller != nullptr)
    {
        AddControllerYawInput(LookAxisVector.X);
        AddControllerPitchInput(LookAxisVector.Y);
    }
}

void ASyncPlayer::SendSyncToServer()
{
    if (!NetSubsystem)
    {
        UE_LOG(LogTemp, Error, TEXT("❌ SendSyncToServer: NetSubsystem is NULL!"));
        ClientFileLog(TEXT("❌ SendSyncToServer: NetSubsystem is NULL!"));
        return;
    }

    FVector Position = GetActorLocation();
    FRotator Rotation = GetActorRotation();
    bool IsFalling = false;
    FString AnimName = TEXT("None");

    if (const UAnimInstance* AnimInstance = GetMesh() ? GetMesh()->GetAnimInstance() : nullptr)
    {
        if (const UAnimMontage* Montage = AnimInstance->GetCurrentActiveMontage())
        {
            AnimName = Montage->GetName();
        }
    }

    const int32 AnimID = AnimName == TEXT("None") ? 0 : UBase36::Base36ToInt(AnimName);

    if (UCharacterMovementComponent* Movement = GetCharacterMovement())
		IsFalling = Movement->IsFalling();

    struct FSyncSnapshot
    {
        FVector Position;
        FRotator Rotation;
        int32 AnimID;
        bool IsFalling;
    };

    FSyncSnapshot Snapshot{ Position, Rotation, AnimID, IsFalling };
    uint32 CurrentHash = FCRC32C::Compute(reinterpret_cast<const uint8*>(&Snapshot), sizeof(Snapshot));

    if (CurrentHash == LastSyncHash)
    {
        // Skip sending if nothing changed
        return;
    }

    LastSyncHash = CurrentHash;
    const FVector Velocity = GetVelocity();

                // Log first few sync packets
    static int32 SyncCount = 0;
    if (SyncCount < 5)
    {
        UE_LOG(LogTemp, Warning, TEXT("📡 SendSyncToServer #%d: Pos=%s, Rot=%s, Vel=%s"),
            SyncCount, *Position.ToString(), *Rotation.ToString(), *Velocity.ToString());
        ClientFileLog(FString::Printf(TEXT("📡 SendSyncToServer #%d: Pos=%s, Rot=%s, Vel=%s"),
            SyncCount, *Position.ToString(), *Rotation.ToString(), *Velocity.ToString()));
        SyncCount++;
    }

    // Check if World Origin Rebasing is enabled
    if (UWorld* World = GetWorld())
    {
        if (UTOSGameInstance* TosGameInstance = Cast<UTOSGameInstance>(World->GetGameInstance()))
        {
            if (TosGameInstance->bEnableWorldOriginRebasing)
            {
                UE_LOG(LogTemp, VeryVerbose, TEXT("📤 Calling NetSubsystem->SendEntitySyncQuantized..."));
                ClientFileLog(TEXT("📤 Calling NetSubsystem->SendEntitySyncQuantized..."));
                NetSubsystem->SendEntitySyncQuantized(Position, Rotation, AnimID, Velocity, IsFalling);
                UE_LOG(LogTemp, VeryVerbose, TEXT("✅ NetSubsystem->SendEntitySyncQuantized completed"));
                ClientFileLog(TEXT("✅ NetSubsystem->SendEntitySyncQuantized completed"));
                return;
            }
        }
    }

    // Fallback to regular sync if World Origin Rebasing is disabled
    UE_LOG(LogTemp, VeryVerbose, TEXT("📤 Calling NetSubsystem->SendEntitySync..."));
    ClientFileLog(TEXT("📤 Calling NetSubsystem->SendEntitySync..."));
    NetSubsystem->SendEntitySync(Position, Rotation, AnimID, Velocity, IsFalling);
    UE_LOG(LogTemp, VeryVerbose, TEXT("✅ NetSubsystem->SendEntitySync completed"));
    ClientFileLog(TEXT("✅ NetSubsystem->SendEntitySync completed"));
}

