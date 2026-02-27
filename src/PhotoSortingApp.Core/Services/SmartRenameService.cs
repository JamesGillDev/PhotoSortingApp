using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PhotoSortingApp.Core.Interfaces;
using PhotoSortingApp.Domain.Models;

namespace PhotoSortingApp.Core.Services;

public class SmartRenameService : ISmartRenameService
{
    private static readonly Dictionary<string, string> SettingHints = new(StringComparer.OrdinalIgnoreCase)
    {
        ["beach"] = "beach",
        ["ocean"] = "beach",
        ["sea"] = "beach",
        ["lake"] = "lake",
        ["river"] = "river",
        ["forest"] = "forest",
        ["woods"] = "forest",
        ["mountain"] = "mountain",
        ["hike"] = "mountain",
        ["park"] = "park",
        ["city"] = "city",
        ["downtown"] = "city",
        ["street"] = "city",
        ["home"] = "home",
        ["house"] = "home",
        ["office"] = "office",
        ["school"] = "school",
        ["indoor"] = "indoor",
        ["outdoor"] = "outdoor",
        ["garden"] = "garden",
        ["stadium"] = "stadium",
        ["restaurant"] = "restaurant"
    };

    private static readonly Dictionary<string, string> PeopleHints = new(StringComparer.OrdinalIgnoreCase)
    {
        ["selfie"] = "selfie",
        ["portrait"] = "portrait",
        ["family"] = "family",
        ["wedding"] = "wedding",
        ["birthday"] = "birthday",
        ["party"] = "party",
        ["friends"] = "friends",
        ["kids"] = "kids",
        ["baby"] = "baby",
        ["group"] = "group"
    };

    private static readonly Dictionary<string, string> SubjectHints = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dog"] = "dog",
        ["cat"] = "cat",
        ["pet"] = "pet",
        ["sunset"] = "sunset",
        ["sunrise"] = "sunrise",
        ["food"] = "food",
        ["travel"] = "travel",
        ["vacation"] = "travel",
        ["car"] = "car",
        ["bike"] = "bike",
        ["flower"] = "flower",
        ["tree"] = "nature",
        ["snow"] = "snow",
        ["beach"] = "beach"
    };

    private static readonly Dictionary<string, string> AnimalHints = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dog"] = "dog",
        ["puppy"] = "dog",
        ["cat"] = "cat",
        ["kitten"] = "cat",
        ["horse"] = "horse",
        ["cow"] = "cow",
        ["bird"] = "bird",
        ["eagle"] = "bird",
        ["duck"] = "duck",
        ["deer"] = "deer",
        ["bear"] = "bear",
        ["rabbit"] = "rabbit",
        ["fish"] = "fish",
        ["pet"] = "pet"
    };

    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly string _chatCompletionsUrl;
    private readonly string? _apiKey;

    public SmartRenameService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(45);

        var baseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL");
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = "https://api.openai.com/v1";
        }

        _chatCompletionsUrl = $"{baseUrl.TrimEnd('/')}/chat/completions";
        _model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";
        _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    }

    public async Task<SmartRenameAnalysis> AnalyzeAsync(
        PhotoAsset photo,
        IReadOnlyList<string> existingTags,
        CancellationToken cancellationToken = default)
    {
        var fallback = BuildHeuristicAnalysis(photo, existingTags);
        if (string.IsNullOrWhiteSpace(_apiKey) || !File.Exists(photo.FullPath))
        {
            return fallback;
        }

        try
        {
            var aiAnalysis = await TryAnalyzeWithVisionAsync(photo, existingTags, cancellationToken).ConfigureAwait(false);
            if (aiAnalysis is null)
            {
                return fallback;
            }

            return MergeAnalyses(fallback, aiAnalysis);
        }
        catch
        {
            return fallback;
        }
    }

    private async Task<SmartRenameAnalysis?> TryAnalyzeWithVisionAsync(
        PhotoAsset photo,
        IReadOnlyList<string> existingTags,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return null;
        }

        var imagePayload = await BuildImagePayloadAsync(photo.FullPath, cancellationToken).ConfigureAwait(false);
        if (imagePayload is null)
        {
            return null;
        }

        var dateLocal = (photo.DateTaken ?? photo.FileLastWriteUtc).ToLocalTime();
        var tagContext = existingTags.Count == 0 ? "(none)" : string.Join(", ", existingTags.Take(12));
        var userPrompt =
            "Analyze this photo for rename metadata. Return STRICT JSON only with fields: " +
            "summary, people, setting, season, holiday, shot_type, time_of_day, subjects (array), " +
            "people_ids (array), animal_ids (array), suggested_base_name. " +
            "Values should be short lowercase words. suggested_base_name must be filesystem-safe and concise." +
            $"{Environment.NewLine}Known metadata: date_local={dateLocal:yyyy-MM-dd HH:mm}, camera={photo.CameraMake} {photo.CameraModel}, existing_tags={tagContext}.";

        var requestBody = new
        {
            model = _model,
            temperature = 0.2,
            max_tokens = 260,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "You produce compact image rename metadata for photo management software."
                },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = userPrompt },
                        new
                        {
                            type = "image_url",
                            image_url = new
                            {
                                url = imagePayload.Value.DataUrl,
                                detail = "low"
                            }
                        }
                    }
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _chatCompletionsUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var message = ExtractAssistantContent(json);
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        var parsedJson = ExtractJsonObject(message) ?? message;
        using var doc = JsonDocument.Parse(parsedJson);
        var root = doc.RootElement;

        var subjectTags = ExtractStringArray(root, "subjects");
        var peopleIds = ExtractStringArray(root, "people_ids");
        var animalIds = ExtractStringArray(root, "animal_ids");
        var analysis = new SmartRenameAnalysis
        {
            Summary = ReadString(root, "summary"),
            PeopleHint = NormalizeToken(ReadString(root, "people")),
            Setting = NormalizeToken(ReadString(root, "setting")),
            Season = NormalizeToken(ReadString(root, "season")),
            Holiday = NormalizeToken(ReadString(root, "holiday")),
            ShotType = NormalizeToken(ReadString(root, "shot_type")),
            TimeOfDay = NormalizeToken(ReadString(root, "time_of_day")),
            SubjectTags = subjectTags,
            DetectedPeople = peopleIds,
            DetectedAnimals = animalIds,
            UsedVisionModel = true
        };

        var baseName = NormalizeFileToken(ReadString(root, "suggested_base_name"));
        analysis.SuggestedBaseName = string.IsNullOrWhiteSpace(baseName)
            ? BuildBaseNameFromAnalysis(analysis, photo)
            : baseName;

        return analysis;
    }

    private static SmartRenameAnalysis BuildHeuristicAnalysis(PhotoAsset photo, IReadOnlyList<string> existingTags)
    {
        var analysis = new SmartRenameAnalysis();
        var localDate = (photo.DateTaken ?? photo.FileLastWriteUtc).ToLocalTime();
        analysis.Season = GetSeason(localDate.Month);
        analysis.TimeOfDay = GetTimeOfDay(localDate.Hour);
        analysis.Holiday = GetHoliday(localDate);
        analysis.ShotType = GetShotType(photo.Width, photo.Height);

        var textBlob = $"{photo.FileName} {photo.FolderPath} {photo.Notes} {string.Join(' ', existingTags)}";
        var tokens = Tokenize(textBlob);

        analysis.Setting = MatchFirst(tokens, SettingHints);
        analysis.PeopleHint = MatchFirst(tokens, PeopleHints);
        var subjects = tokens
            .Select(token => SubjectHints.TryGetValue(token, out var label) ? label : null)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToList();
        analysis.SubjectTags = subjects;
        var detectedPeople = DetectPeopleIds(existingTags, tokens);
        var detectedAnimals = DetectAnimalIds(existingTags, subjects, tokens);
        if (detectedPeople.Count == 0)
        {
            detectedPeople = InferGenericPeopleFromContext(
                analysis.PeopleHint,
                analysis.ShotType,
                detectedAnimals,
                subjects,
                tokens);
        }

        analysis.DetectedPeople = detectedPeople;
        analysis.DetectedAnimals = detectedAnimals;

        analysis.SuggestedBaseName = BuildBaseNameFromAnalysis(analysis, photo);
        analysis.Summary = "Heuristic analysis";
        return analysis;
    }

    private static IReadOnlyList<string> InferGenericPeopleFromContext(
        string? peopleHint,
        string? shotType,
        IReadOnlyList<string> detectedAnimals,
        IReadOnlyList<string> subjectTags,
        IReadOnlyList<string> tokens)
    {
        if (detectedAnimals.Count > 0)
        {
            return Array.Empty<string>();
        }

        var hasAnimalSignals =
            subjectTags.Any(tag => AnimalHints.ContainsKey(tag)) ||
            tokens.Any(token => AnimalHints.ContainsKey(token));
        if (hasAnimalSignals)
        {
            return Array.Empty<string>();
        }

        var portraitLike =
            shotType?.Equals("portrait", StringComparison.OrdinalIgnoreCase) == true ||
            peopleHint?.Equals("portrait", StringComparison.OrdinalIgnoreCase) == true ||
            peopleHint?.Equals("selfie", StringComparison.OrdinalIgnoreCase) == true;
        if (!portraitLike)
        {
            return Array.Empty<string>();
        }

        var isGroupLike = tokens.Any(token =>
            token is "group" or "team" or "friends" or "family" or "wedding" or "party");
        return isGroupLike
            ? new[] { "person_group" }
            : new[] { "person" };
    }

    private static SmartRenameAnalysis MergeAnalyses(SmartRenameAnalysis fallback, SmartRenameAnalysis ai)
    {
        if (string.IsNullOrWhiteSpace(ai.SuggestedBaseName))
        {
            ai.SuggestedBaseName = fallback.SuggestedBaseName;
        }

        if (string.IsNullOrWhiteSpace(ai.Setting))
        {
            ai.Setting = fallback.Setting;
        }

        if (string.IsNullOrWhiteSpace(ai.PeopleHint))
        {
            ai.PeopleHint = fallback.PeopleHint;
        }

        if (string.IsNullOrWhiteSpace(ai.Season))
        {
            ai.Season = fallback.Season;
        }

        if (string.IsNullOrWhiteSpace(ai.Holiday))
        {
            ai.Holiday = fallback.Holiday;
        }

        if (string.IsNullOrWhiteSpace(ai.TimeOfDay))
        {
            ai.TimeOfDay = fallback.TimeOfDay;
        }

        if (string.IsNullOrWhiteSpace(ai.ShotType))
        {
            ai.ShotType = fallback.ShotType;
        }

        if (ai.SubjectTags.Count == 0)
        {
            ai.SubjectTags = fallback.SubjectTags;
        }

        if (ai.DetectedPeople.Count == 0)
        {
            ai.DetectedPeople = fallback.DetectedPeople;
        }

        if (ai.DetectedAnimals.Count == 0)
        {
            ai.DetectedAnimals = fallback.DetectedAnimals;
        }

        ai.SuggestedBaseName = NormalizeFileToken(ai.SuggestedBaseName);
        if (string.IsNullOrWhiteSpace(ai.SuggestedBaseName))
        {
            ai.SuggestedBaseName = fallback.SuggestedBaseName;
        }

        return ai;
    }

    private static string BuildBaseNameFromAnalysis(SmartRenameAnalysis analysis, PhotoAsset photo)
    {
        var localDate = (photo.DateTaken ?? photo.FileLastWriteUtc).ToLocalTime();
        var tokens = new List<string>
        {
            localDate.ToString("yyyyMMdd")
        };

        if (!string.IsNullOrWhiteSpace(analysis.Holiday))
        {
            tokens.Add(analysis.Holiday);
        }
        else if (!string.IsNullOrWhiteSpace(analysis.Season))
        {
            tokens.Add(analysis.Season);
        }

        if (!string.IsNullOrWhiteSpace(analysis.Setting))
        {
            tokens.Add(analysis.Setting);
        }

        if (!string.IsNullOrWhiteSpace(analysis.PeopleHint))
        {
            tokens.Add(analysis.PeopleHint);
        }
        else if (analysis.DetectedPeople.Count > 0)
        {
            tokens.Add(analysis.DetectedPeople[0]);
        }

        if (!string.IsNullOrWhiteSpace(analysis.ShotType) &&
            (analysis.ShotType.Equals("portrait", StringComparison.OrdinalIgnoreCase) ||
             analysis.ShotType.Equals("landscape", StringComparison.OrdinalIgnoreCase)))
        {
            tokens.Add(analysis.ShotType);
        }

        foreach (var subject in analysis.SubjectTags.Take(2))
        {
            tokens.Add(subject);
        }

        if (analysis.DetectedAnimals.Count > 0)
        {
            tokens.Add(analysis.DetectedAnimals[0]);
        }

        var normalized = tokens
            .Select(NormalizeFileToken)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count == 0)
        {
            normalized.Add("photo");
        }

        var baseName = string.Join('_', normalized);
        return baseName.Length <= 80 ? baseName : baseName[..80].Trim('_');
    }

    private static async Task<(string MimeType, string DataUrl)?> BuildImagePayloadAsync(string fullPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var extension = Path.GetExtension(fullPath).ToLowerInvariant();
        var mimeType = extension switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

        var bytes = await File.ReadAllBytesAsync(fullPath, cancellationToken).ConfigureAwait(false);
        if (bytes.Length == 0)
        {
            return null;
        }

        // Limit payload size for responsiveness and API reliability.
        if (bytes.Length > 7_000_000)
        {
            return null;
        }

        var dataUrl = $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}";
        return (mimeType, dataUrl);
    }

    private static string? ExtractAssistantContent(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
        {
            return null;
        }

        var first = choices[0];
        if (!first.TryGetProperty("message", out var message) ||
            !message.TryGetProperty("content", out var content))
        {
            return null;
        }

        return content.ValueKind == JsonValueKind.String ? content.GetString() : null;
    }

    private static string? ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return null;
        }

        return text[start..(end + 1)];
    }

    private static IReadOnlyList<string> ExtractStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return element.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => NormalizeToken(x.GetString()))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToList();
    }

    private static string? ReadString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return element.GetString();
    }

    private static string? MatchFirst(IEnumerable<string> tokens, IReadOnlyDictionary<string, string> dictionary)
    {
        foreach (var token in tokens)
        {
            if (dictionary.TryGetValue(token, out var value))
            {
                return value;
            }
        }

        return null;
    }

    private static IReadOnlyList<string> DetectPeopleIds(IReadOnlyList<string> existingTags, IReadOnlyList<string> tokens)
    {
        var fromTags = ParsePrefixedIds(existingTags, "person")
            .Concat(ParsePrefixedIds(existingTags, "people"))
            .ToList();
        if (fromTags.Count > 0)
        {
            return fromTags;
        }

        var inferred = new List<string>();
        foreach (var token in tokens)
        {
            if (PeopleHints.TryGetValue(token, out var personHint))
            {
                inferred.Add(personHint);
            }

            if (token is "man" or "woman" or "boy" or "girl" or "child" or "kids" or "group")
            {
                inferred.Add(token switch
                {
                    "kids" => "child",
                    _ => token
                });
            }
        }

        return inferred
            .Select(NormalizeToken)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToList();
    }

    private static IReadOnlyList<string> DetectAnimalIds(
        IReadOnlyList<string> existingTags,
        IReadOnlyList<string> subjectTags,
        IReadOnlyList<string> tokens)
    {
        var fromTags = ParsePrefixedIds(existingTags, "animal");
        if (fromTags.Count > 0)
        {
            return fromTags;
        }

        var inferred = new List<string>();
        foreach (var subject in subjectTags)
        {
            if (AnimalHints.ContainsKey(subject))
            {
                inferred.Add(subject);
            }
        }

        foreach (var token in tokens)
        {
            if (AnimalHints.TryGetValue(token, out var animalId))
            {
                inferred.Add(animalId);
            }
        }

        return inferred
            .Select(NormalizeToken)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToList();
    }

    private static IReadOnlyList<string> ParsePrefixedIds(IReadOnlyList<string> existingTags, string prefix)
    {
        if (existingTags.Count == 0)
        {
            return Array.Empty<string>();
        }

        var marker = $"{prefix}:";
        return existingTags
            .Select(tag => tag.Trim())
            .Where(tag => tag.StartsWith(marker, StringComparison.OrdinalIgnoreCase))
            .Select(tag => tag[marker.Length..])
            .Select(NormalizeToken)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToList();
    }

    private static IReadOnlyList<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        var separators = new[] { ' ', '\t', '\r', '\n', '.', ',', ';', ':', '/', '\\', '-', '_', '(', ')', '[', ']' };
        return text.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => token.ToLowerInvariant())
            .Where(token => token.Length >= 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string GetSeason(int month)
    {
        return month switch
        {
            12 or 1 or 2 => "winter",
            3 or 4 or 5 => "spring",
            6 or 7 or 8 => "summer",
            _ => "fall"
        };
    }

    private static string GetTimeOfDay(int hour)
    {
        return hour switch
        {
            >= 5 and < 11 => "morning",
            >= 11 and < 16 => "afternoon",
            >= 16 and < 21 => "evening",
            _ => "night"
        };
    }

    private static string? GetHoliday(DateTime localDate)
    {
        if (localDate.Month == 12 && localDate.Day >= 20 && localDate.Day <= 31)
        {
            return "christmas";
        }

        if (localDate.Month == 10 && localDate.Day >= 25 && localDate.Day <= 31)
        {
            return "halloween";
        }

        if (localDate.Month == 11 && localDate.Day >= 22 && localDate.Day <= 28)
        {
            return "thanksgiving";
        }

        if (localDate.Month == 1 && localDate.Day <= 3)
        {
            return "new_year";
        }

        if (localDate.Month == 2 && localDate.Day == 14)
        {
            return "valentines";
        }

        if (localDate.Month == 7 && localDate.Day == 4)
        {
            return "independence_day";
        }

        return null;
    }

    private static string GetShotType(int? width, int? height)
    {
        if (!width.HasValue || !height.HasValue || width <= 0 || height <= 0)
        {
            return "photo";
        }

        if (height > width)
        {
            return "portrait";
        }

        if (width > height)
        {
            return "landscape";
        }

        return "square";
    }

    private static string? NormalizeToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = NormalizeFileToken(value);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string NormalizeFileToken(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var chars = raw
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_')
            .ToArray();
        var token = string.Join('_', new string(chars)
            .Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        if (token.Length > 32)
        {
            token = token[..32];
        }

        return token;
    }
}
