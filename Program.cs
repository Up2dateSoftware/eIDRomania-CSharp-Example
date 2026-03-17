using EIDRomania.SDK;
using EIDRomania.SDK.Api;

/// <summary>
/// eIDRomania Desktop SDK — C# Example Application
///
/// Demonstrates:
///   - SDK initialization with a license key
///   - Listing connected PC/SC readers
///   - Reading a Romanian eID card with CAN only (MRTD data + photo)
///   - Reading a Romanian eID card with CAN + PIN (full data including address)
///   - Typed error handling for every failure scenario
///   - Progress callbacks
///
/// Prerequisites:
///   - A PC/SC smart card reader (contact or NFC/contactless)
///   - A Romanian electronic identity card (CEI)
///   - A valid eIDRomania Desktop SDK license key
///
/// Run:
///   dotnet run
/// </summary>

// Replace with your actual license key issued by Up2Date Software SRL.
// For testing, contact: office@up2date.ro
const string LICENSE_KEY = "YOUR_LICENSE_KEY_HERE";
const string APP_IDENTIFIER = "com.example.eid";

Console.WriteLine("=================================================");
Console.WriteLine("  eIDRomania Desktop SDK — C# Example");
Console.WriteLine("=================================================");
Console.WriteLine();

using var sdk = new EIDRomaniaSdk();

try
{
    // ── Step 1: Initialize ──────────────────────────────────────────────────
    Console.WriteLine("[1/4] Initializing SDK...");
    sdk.Initialize(LICENSE_KEY, APP_IDENTIFIER);

    if (sdk.LicenseInfo != null)
    {
        Console.WriteLine($"      License valid. Issued to: {sdk.LicenseInfo.IssuedTo}");
        Console.WriteLine($"      Expires: {sdk.LicenseInfo.ExpiresAt}");
    }

    // ── Step 2: List readers ────────────────────────────────────────────────
    Console.WriteLine();
    Console.WriteLine("[2/4] Listing PC/SC readers...");
    var readers = sdk.AvailableReaders;

    Console.WriteLine($"      Found {readers.Count} reader(s):");
    foreach (var reader in readers)
    {
        var cardStatus = reader.HasCard ? "card present" : "no card";
        Console.WriteLine($"      [{reader.Index}] {reader.Name} — {cardStatus}");
    }

    // Select the first reader with a card, or default to index 0
    var selectedIndex = 0;
    foreach (var reader in readers)
    {
        if (reader.HasCard)
        {
            selectedIndex = reader.Index;
            break;
        }
    }
    Console.WriteLine($"      Using reader index: {selectedIndex}");
    sdk.SetActiveReader(selectedIndex);

    // ── Step 3: Read card with CAN only ─────────────────────────────────────
    Console.WriteLine();
    Console.Write("[3/4] Enter CAN (6 digits from card front, or press Enter to skip): ");
    var can = Console.ReadLine()?.Trim() ?? "";

    if (!string.IsNullOrEmpty(can))
    {
        Console.WriteLine("      Reading card with CAN only (MRTD data + face photo)...");
        Console.WriteLine("      Keep the card on the reader until reading is complete.");
        Console.WriteLine();

        var card = await sdk.ReadAsync(can, null, (msg, pct) =>
        {
            Console.Write($"\r      [{pct,3}%] {msg,-50}");
        });
        Console.WriteLine();
        Console.WriteLine();

        Console.WriteLine("-- CAN-only read result ----------------------------------------");
        PrintCard(card);

        // ── Step 4: Read card with CAN + PIN ────────────────────────────────
        Console.WriteLine();
        Console.Write("[4/4] Enter PIN (4 digits) for full data (or press Enter to skip): ");
        var pin = Console.ReadLine()?.Trim() ?? "";

        if (!string.IsNullOrEmpty(pin))
        {
            Console.WriteLine("      Reading card with CAN + PIN (full data including address)...");
            Console.WriteLine("      Keep the card on the reader until reading is complete.");
            Console.WriteLine();

            var fullCard = await sdk.ReadAsync(can, pin, (msg, pct) =>
            {
                Console.Write($"\r      [{pct,3}%] {msg,-50}");
            });
            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine("-- CAN+PIN read result -----------------------------------------");
            PrintCard(fullCard);
        }
    }
}
catch (EIDReadException ex)
{
    HandleEIDError(ex);
}
catch (Exception ex) when (ex.GetType().Name.Contains("License"))
{
    Console.Error.WriteLine($"[ERROR] License error: {ex.Message}");
    Console.Error.WriteLine("        Contact Up2Date Software SRL for a valid license key.");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[ERROR] Unexpected error: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
}

Console.WriteLine();
Console.WriteLine("SDK closed. Goodbye.");

// ── Helper methods ─────────────────────────────────────────────────────────

static void HandleEIDError(EIDReadException e)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine($"[ERROR] Card reading failed — {e.ErrorCode}");
    Console.Error.WriteLine($"        {e.Message}");
    Console.Error.WriteLine();

    switch (e.ErrorCode)
    {
        case ErrorCode.NotInitialized:
            Console.Error.WriteLine("  -> Call sdk.Initialize(licenseKey, appIdentifier) before any other method.");
            break;

        case ErrorCode.NoReader:
            Console.Error.WriteLine("  -> Connect a PC/SC smart card reader (contact or NFC/contactless).");
            Console.Error.WriteLine("  -> Install the reader driver (check manufacturer website).");
            Console.Error.WriteLine("  -> On Windows: verify the Smart Card service is running.");
            break;

        case ErrorCode.NoCard:
            Console.Error.WriteLine("  -> Place the Romanian eID card on the reader and try again.");
            break;

        case ErrorCode.TagLost:
            Console.Error.WriteLine("  -> The card was removed or moved during reading.");
            Console.Error.WriteLine("  -> Keep the card still on the reader for the entire ~10 seconds.");
            Console.Error.WriteLine("  -> On Windows with ACS ACR1252: physically remove and reinsert the card.");
            break;

        case ErrorCode.InvalidCan:
            Console.Error.WriteLine("  -> The CAN (Card Access Number) is incorrect.");
            Console.Error.WriteLine("  -> Find the 6-digit CAN on the front of the card, near the photo.");
            Console.Error.WriteLine("  -> CAN is NOT the same as the PIN.");
            break;

        case ErrorCode.InvalidPin:
            if (e.AttemptsRemaining > 0)
            {
                Console.Error.WriteLine($"  -> The PIN is incorrect. {e.AttemptsRemaining} attempt(s) remaining.");
                if (e.AttemptsRemaining == 1)
                {
                    Console.Error.WriteLine("  -> WARNING: Only 1 attempt left! Do NOT retry unless you are certain.");
                    Console.Error.WriteLine("  -> If the next attempt fails, the card will be permanently locked.");
                }
            }
            else
            {
                Console.Error.WriteLine("  -> The PIN is incorrect.");
            }
            Console.Error.WriteLine("  -> The PIN is 4 digits, set by the card owner at the DEP office.");
            break;

        case ErrorCode.CardLocked:
            Console.Error.WriteLine("  -> The card is permanently locked due to too many failed PIN attempts.");
            Console.Error.WriteLine("  -> The card owner must visit a DEP (Directia Evidenta Persoanelor) office to unlock it.");
            Console.Error.WriteLine("  -> NOTE: The card can still be read without PIN (CAN-only mode).");
            break;

        case ErrorCode.Timeout:
            Console.Error.WriteLine("  -> Communication with the card timed out.");
            Console.Error.WriteLine("  -> Ensure the card is firmly placed on the reader.");
            Console.Error.WriteLine("  -> Try again, or restart the reader.");
            break;

        case ErrorCode.UnsupportedCard:
            Console.Error.WriteLine("  -> This card is not a supported Romanian eID card.");
            Console.Error.WriteLine("  -> Only Romanian electronic identity cards (CEI) are supported.");
            Console.Error.WriteLine("  -> Romanian passports and older non-electronic IDs are not supported.");
            break;

        case ErrorCode.ReadFailure:
            Console.Error.WriteLine("  -> Failed to read card data. This may be a temporary issue.");
            Console.Error.WriteLine("  -> Remove and reinsert the card, then try again.");
            Console.Error.WriteLine("  -> On Windows with ACS ACR1252: this is expected on first placement.");
            Console.Error.WriteLine("     Remove and reinsert the card to resolve.");
            break;

        default:
            Console.Error.WriteLine("  -> Unexpected error. Check the cause for details.");
            if (e.InnerException != null)
                Console.Error.WriteLine($"  -> Cause: {e.InnerException.Message}");
            break;
    }
}

static void PrintCard(EIDCard card)
{
    Console.WriteLine("  Personal data:");
    PrintField("    Surname", card.Surname);
    PrintField("    Given names", card.GivenNames);
    PrintField("    CNP", card.Cnp);
    PrintField("    Date of birth", card.DateOfBirth);
    PrintField("    Sex", card.Sex);
    PrintField("    Nationality", card.Nationality);
    PrintField("    Place of birth", card.PlaceOfBirth);

    Console.WriteLine("  Document:");
    PrintField("    Document number", card.DocumentNumber);
    PrintField("    Series", card.DocumentSeries);
    PrintField("    Date of issue", card.DateOfIssue);
    PrintField("    Date of expiry", card.DateOfExpiry);
    PrintField("    Issuing authority", card.IssuingAuthority);

    Console.WriteLine("  Address:");
    Console.WriteLine(card.PermanentAddress != null
        ? $"    {card.PermanentAddress}"
        : "    (not available — requires PIN)");

    Console.WriteLine("  Biometrics:");
    Console.WriteLine(card.FacialImageBase64 != null
        ? $"    Face photo:  available ({card.FacialImageBase64.Length} chars base64)"
        : "    Face photo:  not available");
    Console.WriteLine(card.SignatureImageBase64 != null
        ? $"    Signature:   available ({card.SignatureImageBase64.Length} chars base64)"
        : "    Signature:   not available");

    Console.WriteLine("  Authentication:");
    if (card.AuthenticationResult is AuthenticationResult.Authentic)
        Console.WriteLine("    Card is authentic (Passive Authentication passed)");
    else if (card.AuthenticationResult is AuthenticationResult.Warning w)
        Console.WriteLine($"    Warning: {w.Message} — {w.Details}");
    else if (card.AuthenticationResult is AuthenticationResult.Failed f)
        Console.WriteLine($"    FAILED: {f.Reason} — {f.Details}");
    else
        Console.WriteLine("    (not available — SOD not read)");
}

static void PrintField(string label, string? value)
{
    Console.WriteLine($"{label,-24} {value ?? "(not available)"}");
}
