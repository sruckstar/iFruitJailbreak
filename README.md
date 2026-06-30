# iFruit Jailbreak

A C# library for GTA V (ScriptHookVDotNet) that injects SMS messages and emails directly into the player's in-game cellphone via low-level global variable memory manipulation.

## Overview

GTA V's cellphone controller script manages a set of global variable arrays for storing SMS messages and email feed entries. **iFruit Jailbreak** writes directly to these arrays and sets the appropriate bit flags, causing the native cellphone functions to process the injected data exactly as they would for real game messages.

The result is indistinguishable from a message sent by the game itself:

- Native notification styling and colors
- Proper character icon rendering
- SMS/email arrival sound
- Gamepad vibration (SMS only)
- Inbox persistence across frames

## Features

- Send SMS messages from any built-in GTA V character (Franklin, Michael, Trevor)
- Send email notifications with header, body, and sender
- Inject emails into the per-character inbox (persistent)
- Delete injected SMS messages by slot ID
- Safe multi-frame message splitting for long texts (up to 3 × 63 UTF-8 bytes)

## Requirements

| Component | Version |
|---|---|
| .NET Framework | 4.8 |
| ScriptHookVDotNet | 3.x (`ScriptHookVDotNet3.dll`) |
| GTA V | version 1.72 or later |

## Installation

1. Copy `iFruit Jailbreak.dll` to your GTA V `scripts/` folder
3. Add `using iFruitJailbreak;` in your script file.

## Quick Start

```csharp
using iFruitJailbreak;

// SMS: messageText, senderID
appTextMessage.Send("Hey, what's up?", 2); //ID 2 - Franklin. See WIKI.

// Email: GXT key ID of the preview text in the notification, GXT key ID of the email body, sender ID (see WIKI)
appMail.SendStrID(33, 33, 10);
```

See [WIKI](https://github.com/sruckstar/iFruitJailbreak/wiki) for the full API reference.

## Project Structure

```
iFruit Jailbreak/
├── appMail.cs              Email injection API and feed rendering
├── appTextMessage.cs       Public wrapper around SmsInjector
├── GtaCharacter.cs         Character enum (Franklin / Michael / Trevor / NoCharacter)
├── GtaMemory.cs            All global variable offsets and index constants
├── SmsInjector.cs          Core SMS injection engine
├── SmsMessage.cs           SMS data model
└── example.cs              Usage examples
```

## How It Works

GTA V's `cellphone_controller` script keeps its state in numbered global variables. iFruit Jailbreak:

1. Locates a free slot in the SMS or email feed array.
2. Writes the message data (text chunks, sender ID, timestamp, lock flag).
3. Updates a small set of single-integer globals (current slot index, sender ID, headshot ID).
4. Flips the bit flags that the native cellphone controller checks each frame (`Global_9463` for SMS, `Global_4524844` for email).
5. Plays the arrival tone via `PLAY_SOUND_FRONTEND`.

The game engine then calls its own rendering functions (`func_65` for SMS feed, `func_60` for email feed) using the data already written, so no UI code is needed in the mod.

## SMS Slot Limits

The SMS array holds **165 slots** (indices 0–164). When all slots are occupied `Send()` returns `-1`. Call `appTextMessage.Delete(slotId)` to free a slot manually if needed.
