using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using PowerMath.Diagnostics;

namespace PowerMath.Session
{
    public enum DisplayNameDenialReason
    {
        None = 0,
        EmptyOrWhitespace,
        TooLong,
        InvalidCharacters,
        InappropriateContent,
        ContactInformation
    }

    public readonly struct DisplayNameValidationResult
    {
        public bool IsValid => Reason == DisplayNameDenialReason.None;
        public DisplayNameDenialReason Reason { get; }
        public string LocalizationKey { get; }
        public string FlaggedTerm { get; }

        public DisplayNameValidationResult(DisplayNameDenialReason reason, string localizationKey, string flaggedTerm = null)
        {
            Reason = reason;
            LocalizationKey = localizationKey;
            FlaggedTerm = flaggedTerm;
        }

        public static DisplayNameValidationResult Success() =>
            new DisplayNameValidationResult(DisplayNameDenialReason.None, string.Empty);

        public static DisplayNameValidationResult Denied(DisplayNameDenialReason reason, string localizationKey, string flaggedTerm = null) =>
            new DisplayNameValidationResult(reason, localizationKey, flaggedTerm);
    }

    /// <summary>
    /// Industry-standard kid-friendly display name moderation and policy engine.
    /// Filters profanity, toxicity, vulgarity, nudity/sexual content, and PII/contact details in English and Thai.
    /// Handles Thai delimiter evasion, spacing bypasses, leetspeak, homoglyphs, repeated letter stretching,
    /// and Scunthorpe allowlisting.
    /// </summary>
    public static class DisplayNamePolicy
    {
        public const int MaximumDisplayNameLength = 20;
        public const int MinimumDisplayNameLength = 1;

        private static readonly Regex ContactPhoneRegex = new(
            @"(?:(?:\+?66|0)[ -]?[2-9][0-9]{1}[ -]?[0-9]{3}[ -]?[0-9]{4})|(?:\b\d{3}[ -]\d{3}[ -]\d{4}\b)|(?:\b0[689]\d{8}\b)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex ContactLinkRegex = new(
            @"(?:https?:\/\/|www\.)|(?:discord(?:\.gg|\.com\/invite))|(?:[a-zA-Z0-9-]+\.(?:com|net|org|io|me|xyz|th|gg)(?:\/|\b))",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex ConsecutiveRepeatsRegex = new(
            @"(.)\1{2,}",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Safe words that contain substrings that might otherwise trigger false positives (Scunthorpe problem)
        private static readonly HashSet<string> ScunthorpeAllowList = new(StringComparer.OrdinalIgnoreCase)
        {
            "pass", "passage", "passport", "compass",
            "classic", "classical", "class", "classroom",
            "assistant", "assist", "assistance",
            "butter", "button", "butterfly",
            "grass", "glass", "mass", "bass",
            "title", "entity", "titan",
            "document", "cucumber",
            "hello", "shell",
            "ricko", "stellar", "power", "math", "powermath",
            "hero", "super", "knight", "champion", "master", "legend",
            "ผู้กล้า", "สเตลลาร์", "ริกโก้", "หีบ", "หีบสมบัติ", "หีบเพลง",
            "กูเกิ้ล", "กูเกิล", "กูรู", "หอยทาก", "ตีนเขา"
        };

        // Standard English profanity, vulgarity, sexual/nudity, and toxic keywords
        private static readonly string[] EnglishProhibitedTerms =
        {
            "fuck", "fuk", "fck", "fuc", "shit", "bitch", "btch", "cunt", "dick", "dck",
            "cock", "pussy", "asshole", "bastard", "slut", "whore", "porn", "prn",
            "nude", "nudity", "naked", "boobs", "penis", "vagina", "anal", "dildo",
            "pedophile", "pedo", "paedo", "nazi", "hitler", "faggot", "retard",
            "sex", "sexy", "sexx", "horny", "blowjob", "handjob", "cum", "masturbat",
            "rape", "rapist", "tits", "titties", "kys", "kill yourself", "suicide",
            "terrorist", "jihad", "nigga", "nigger"
        };

        // Standard Thai profanity, vulgarity, sexual/nudity, and toxic keywords
        // Categorized according to Thai Content Moderation & Digital Economy standards (ETDA / Thai Online Games)
        private static readonly string[] ThaiProhibitedTerms =
        {
            // 1. Female anatomy & sexual terms
            "หี", "หิ", "หี่", "หีย์", "แตด", "จิ๋ม", "ช่องคลอด", "แคม", "ร่องสวาท",
            "กะหรี่", "กระหรี่", "ซ่อง", "ขายตัว", "ขายบริการ", "โสเภณี",

            // 2. Male anatomy & sexual terms
            "ควย", "ควe", "ควัย", "โคย", "ดอ", "กระดอ", "หำ", "หํา", "จู๋", "เจี๊ยว",
            "ลึงค์", "ไข่ห้อย", "หัวควย", "หัวดอ", "หัวแตด", "หัวหำ",

            // 3. Sexual acts & lust
            "เย็ด", "เยด", "เยส", "เด้า", "กระเด้า", "เงี่ยน", "ร่าน", "เสียว",
            "ชักว่าว", "น้ำแตก", "อมนกเขา", "ดูดควย", "เลียหี", "เลียแตด", "ข่มขืน",

            // 4. Severe curse words & animal insults
            "เหี้ย", "ไอ้เหี้ย", "อีเหี้ย", "ไอ่เหี้ย", "เชี่ย", "ไอ้เชี่ย", "อีเชี่ย",
            "สัส", "สัด", "สึด", "สาส", "สาซ", "ไอ้สัส", "อีสัส", "ไอ้สัตว์", "อีสัตว์",
            "ดอกทอง", "อีดอก", "อีดอกทอง", "ชาติหมา", "ไอ้ชาติหมา", "อีชาติหมา",
            "จัญไร", "อัปปรีย์", "ระยำ", "สารเลว", "สถุล", "สันดาน", "หน้าด้าน",
            "ตอแหล", "บัดซบ", "เสือก", "ส้นตีน", "กวนตีน", "ไอ้ควาย", "อีควาย",
            "แม่ง", "มึง", "กู", "ห่า", "ไอ้ห่า", "อีห่า", "เปรต", "ไอ้เวร", "อีเวร", "ไอ้บ้า",
            "ลูกกะหรี่", "พ่อมึงตาย", "แม่มึงตาย", "โคตรพ่อ", "โคตรแม่", "ยัดแม่",
            "ชิบหาย", "ฉิบหาย"
        };

        // Phonetic / karaoke Thai slang terms
        private static readonly string[] ThaiKaraokeProhibitedTerms =
        {
            "kuy", "kuay", "hee", "heee", "hiea", "hia", "chia", "sat", "sud", "yed", "ted", "mung", "dorkthong", "karhee"
        };

        public static bool TryValidate(string input, out string normalizedName, out DisplayNameValidationResult result)
        {
            normalizedName = (input ?? string.Empty).Normalize(NormalizationForm.FormC).Trim();

            // 1. Empty / Whitespace check
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                result = DisplayNameValidationResult.Denied(
                    DisplayNameDenialReason.EmptyOrWhitespace,
                    "onboarding.invalidName");
                return false;
            }

            // 2. Length check via StringInfo (Text elements / grapheme clusters)
            var stringInfo = new StringInfo(normalizedName);
            if (stringInfo.LengthInTextElements > MaximumDisplayNameLength)
            {
                result = DisplayNameValidationResult.Denied(
                    DisplayNameDenialReason.TooLong,
                    "onboarding.nameTooLong");
                return false;
            }

            // 3. Character security check (no control chars, no HTML markup)
            foreach (char c in normalizedName)
            {
                if (char.IsControl(c) || c == '<' || c == '>')
                {
                    result = DisplayNameValidationResult.Denied(
                        DisplayNameDenialReason.InvalidCharacters,
                        "onboarding.invalidName");
                    return false;
                }
            }

            // 4. Contact / PII protection (COPPA child safety)
            if (ContactPhoneRegex.IsMatch(normalizedName) || ContactLinkRegex.IsMatch(normalizedName))
            {
                AppLog.Warning("DisplayNamePolicy", $"Name '{normalizedName}' denied: Contains contact or PII pattern.");
                result = DisplayNameValidationResult.Denied(
                    DisplayNameDenialReason.ContactInformation,
                    "onboarding.nameContactInfo");
                return false;
            }

            // 5. Scunthorpe / Allowlist check (exact match on safe name)
            if (ScunthorpeAllowList.Contains(normalizedName))
            {
                result = DisplayNameValidationResult.Success();
                return true;
            }

            // 6. Kid-Friendly Appropriateness Filter (English & Thai)
            if (CheckInappropriateContent(normalizedName, out string flagged))
            {
                AppLog.Warning("DisplayNamePolicy", $"Name '{normalizedName}' denied: Prohibited word detected ('{flagged}').");
                result = DisplayNameValidationResult.Denied(
                    DisplayNameDenialReason.InappropriateContent,
                    "onboarding.nameInappropriate",
                    flagged);
                return false;
            }

            result = DisplayNameValidationResult.Success();
            return true;
        }

        private static bool CheckInappropriateContent(string input, out string flaggedTerm)
        {
            flaggedTerm = null;

            // 1. Check Thai prohibited terms against raw input, delimiter-stripped Thai, and collapsed repeats
            string strippedThai = CanonicalizeThai(input);
            string collapsedThai = ConsecutiveRepeatsRegex.Replace(strippedThai, "$1");

            foreach (string thaiTerm in ThaiProhibitedTerms)
            {
                if (MatchesThaiTerm(input, strippedThai, collapsedThai, thaiTerm))
                {
                    flaggedTerm = thaiTerm;
                    return true;
                }
            }

            // 2. Normalized Latin check for English & Thai Karaoke
            string normalizedLatin = CanonicalizeForFilter(input);

            // Check English prohibited terms
            foreach (string term in EnglishProhibitedTerms)
            {
                if (MatchesTerm(normalizedLatin, input, term))
                {
                    flaggedTerm = term;
                    return true;
                }
            }

            // Check Thai Karaoke terms
            foreach (string term in ThaiKaraokeProhibitedTerms)
            {
                if (MatchesTerm(normalizedLatin, input, term))
                {
                    flaggedTerm = term;
                    return true;
                }
            }

            // Check collapsed repeats version (e.g. fuuuuck -> fuck)
            string collapsedLatin = ConsecutiveRepeatsRegex.Replace(normalizedLatin, "$1$1");
            string singleCollapsedLatin = ConsecutiveRepeatsRegex.Replace(normalizedLatin, "$1");

            foreach (string term in EnglishProhibitedTerms)
            {
                if (MatchesTerm(collapsedLatin, input, term) || MatchesTerm(singleCollapsedLatin, input, term))
                {
                    flaggedTerm = term;
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesThaiTerm(string rawInput, string strippedThai, string collapsedThai, string thaiTerm)
        {
            if (string.IsNullOrEmpty(thaiTerm)) return false;

            bool found = rawInput.IndexOf(thaiTerm, StringComparison.OrdinalIgnoreCase) >= 0 ||
                         strippedThai.IndexOf(thaiTerm, StringComparison.OrdinalIgnoreCase) >= 0 ||
                         collapsedThai.IndexOf(thaiTerm, StringComparison.OrdinalIgnoreCase) >= 0;

            if (!found) return false;

            // Scunthorpe / innocent compound word exceptions for Thai:
            if (thaiTerm == "หี")
            {
                // If occurrence is part of "หีบ" (e.g. "หีบ", "หีบสมบัติ", "หีบเพลง"), exempt unless another "หี" exists
                if (rawInput.IndexOf("หีบ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    strippedThai.IndexOf("หีบ", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string clean = strippedThai.Replace("หีบ", "");
                    if (clean.IndexOf("หี", StringComparison.OrdinalIgnoreCase) < 0)
                        return false;
                }
            }
            else if (thaiTerm == "กู")
            {
                if (rawInput.IndexOf("กูเกิ้ล", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    rawInput.IndexOf("กูเกิล", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    rawInput.IndexOf("กูรู", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string clean = strippedThai.Replace("กูเกิ้ล", "").Replace("กูเกิล", "").Replace("กูรู", "");
                    if (clean.IndexOf("กู", StringComparison.OrdinalIgnoreCase) < 0)
                        return false;
                }
            }
            else if (thaiTerm == "หอย")
            {
                if (rawInput.IndexOf("หอยทาก", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string clean = strippedThai.Replace("หอยทาก", "");
                    if (clean.IndexOf("หอย", StringComparison.OrdinalIgnoreCase) < 0)
                        return false;
                }
            }

            return true;
        }

        private static bool MatchesTerm(string searchTarget, string originalInput, string prohibitedTerm)
        {
            if (string.IsNullOrEmpty(searchTarget) || string.IsNullOrEmpty(prohibitedTerm)) return false;

            int index = searchTarget.IndexOf(prohibitedTerm, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return false;

            // Short prohibited words (<= 3 chars like sex, ass, tit, cum) must use word-boundary or isolated matching
            // to avoid Scunthorpe false positives (e.g. pass, classic, assistant, title).
            if (prohibitedTerm.Length <= 3)
            {
                bool startBound = index == 0 || !char.IsLetterOrDigit(searchTarget[index - 1]);
                bool endBound = (index + prohibitedTerm.Length >= searchTarget.Length) ||
                                !char.IsLetterOrDigit(searchTarget[index + prohibitedTerm.Length]);

                if (startBound && endBound) return true;

                // If embedded, check if original matches any known allowlist word
                foreach (string safeWord in ScunthorpeAllowList)
                {
                    if (originalInput.IndexOf(safeWord, StringComparison.OrdinalIgnoreCase) >= 0)
                        return false;
                }

                return false;
            }

            // For longer words (>= 4 chars like fuck, shit, bitch, porn), check if embedded in an allowlisted safe word
            foreach (string safeWord in ScunthorpeAllowList)
            {
                if (originalInput.IndexOf(safeWord, StringComparison.OrdinalIgnoreCase) >= 0)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Strips whitespace, zero-width characters, and delimiter punctuation between Thai characters
        /// to defeat spacing/symbol evasion (e.g., "ค ว ย", "ค-ว-ย", "ห ี", "ควย / หี" -> "ควยหี").
        /// </summary>
        public static string CanonicalizeThai(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var sb = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                // Skip zero-width and invisible formatting characters
                if (c == '\u200B' || c == '\u200C' || c == '\u200D' || c == '\uFEFF')
                    continue;

                // Skip whitespace and common evasion delimiters
                if (char.IsWhiteSpace(c) || c == '.' || c == '_' || c == '-' || c == '/' ||
                    c == '\\' || c == '*' || c == '~' || c == '^' || c == '|' || c == '+' ||
                    c == '@' || c == '#' || c == '$' || c == '!' || c == ':' || c == ';' ||
                    c == ',' || c == '?' || c == '%' || c == '&' || c == '(' || c == ')' ||
                    c == '[' || c == ']' || c == '{' || c == '}')
                    continue;

                sb.Append(c);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Normalizes text by removing zero-width chars, converting leetspeak, and removing delimiter noise.
        /// </summary>
        public static string CanonicalizeForFilter(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var sb = new StringBuilder(text.Length);
            foreach (char rawChar in text)
            {
                // Skip zero-width and invisible formatting characters
                if (rawChar == '\u200B' || rawChar == '\u200C' || rawChar == '\u200D' || rawChar == '\uFEFF')
                    continue;

                // Skip common evasion delimiters
                if (rawChar == '.' || rawChar == '_' || rawChar == '-' || rawChar == '*' ||
                    rawChar == '~' || rawChar == '^' || rawChar == ' ' || rawChar == '|' ||
                    rawChar == '/' || rawChar == '\\')
                    continue;

                char c = char.ToLowerInvariant(rawChar);

                // Leetspeak / homoglyph substitution mapping
                switch (c)
                {
                    case '@':
                    case '4':
                        sb.Append('a');
                        break;
                    case '8':
                        sb.Append('b');
                        break;
                    case '3':
                        sb.Append('e');
                        break;
                    case '1':
                    case '!':
                        sb.Append('i');
                        break;
                    case '0':
                        sb.Append('o');
                        break;
                    case '$':
                    case '5':
                        sb.Append('s');
                        break;
                    case '7':
                    case '+':
                        sb.Append('t');
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }
    }
}
