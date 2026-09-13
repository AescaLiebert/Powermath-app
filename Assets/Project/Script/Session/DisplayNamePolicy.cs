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
            @"(?:https?:\/\/|www\.)|(?:discord(?:\.gg|\.com\/invite))|(?:[a-zA-Z0-9-]+\.(?:com|net|org|io|me|xyz|th|gg|tv|co|link|app|live|shop|online|site|cc|info|edu|gov)(?:[\/:?]|\b))",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex SocialHandleRegex = new(
            @"(@[a-zA-Z0-9_.-]+)|(?:\b(?:ig|fb|line|dc|tt|yt|tw|discord|roblox|tiktok|steam|snap|snapchat|telegram|tg)\s*[:=]\s*\S+)|(?:\b\S+#\d{4}\b)",
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
            "grape", "grapes", "grapefruit", "drape", "scrape",
            "success", "successful", "succulent", "succumb", "succinct",
            "dictionary", "dictate", "addict", "predict", "verdict", "indicate", "dedicate",
            "fuchsia", "asset", "assert", "assemble", "assembly",
            "ผู้กล้า", "สเตลลาร์", "ริกโก้", "หีบ", "หีบสมบัติ", "หีบเพลง",
            "กูเกิ้ล", "กูเกิล", "กูรู", "หอยทาก", "ตีนเขา",
            "ฟักทอง", "ต้มฟัก", "แกงฟัก", "ฟักเขียว", "ฟักไข่",
            "คัมภีร์", "คัมภีร์เวท", "ดิกชันนารี", "ค็อกเทล", "ค็อกพิท",
            "เทคนิค", "ภาคยานุวัติ", "ผู้ฆ่ามังกร", "ปัญญา"
        };

        // Standard English profanity, vulgarity, sexual/nudity, toxic, hate speech, threats, and assault
        private static readonly string[] EnglishProhibitedTerms =
        {
            // 1. Core profanity, vulgarity, and shortened abbreviations
            "fuck", "fuk", "fck", "fuc", "fvck", "fking", "fk",
            "shit", "sh!t", "shyt",
            "bitch", "btch", "bch", "biatch",
            "cunt", "cnt",
            "dick", "dck", "dic", "dik", "d!ck",
            "cock", "pussy", "asshole", "bastard", "slut", "whore",
            "porn", "prn", "nude", "nudity", "naked", "boobs", "penis", "vagina", "anal", "dildo",
            "sex", "sexy", "sexx", "horny", "blowjob", "handjob", "cum", "masturbat",
            "tits", "titties",
            "twat", "wank", "wanker", "prick",
            "ass", "azz", "a$$", "dumbass", "jackass", "dipshit", "dickhead", "motherfucker",
            "suck", "suc", "suk", "sux",
            "stfu", "gtfo", "wtf",

            // 2. Hate speech, racism, homophobia, slurs
            "nigger", "nigga", "n1gger", "n1gga", "nig",
            "faggot", "fag", "f@g", "dyke", "tranny",
            "chink", "gook", "spic", "kike", "coon",
            "retard", "retarded", "tard",

            // 3. Threats, violence, death wishes, self-harm
            "kys", "kill yourself", "killyourself", "kill urself", "killurself",
            "suicide", "go die", "godie", "kill you", "killyou", "murder you", "shoot you",
            "slit your throat", "hang yourself", "die in a fire",

            // 4. Assault, sexual abuse, pedophilia
            "rape", "rapist", "molest", "molester", "sexual assault",
            "pedophile", "pedo", "paedo", "child predator",

            // 5. Extremism, terrorism
            "nazi", "hitler", "kkk", "terrorist", "jihad", "isis", "taliban",
            "school shooter", "massacre"
        };

        // Standard Thai profanity, vulgarity, sexual/nudity, toxic, hate speech, threats, and assault
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
            "ชักว่าว", "น้ำแตก", "อมนกเขา", "ดูดควย", "เลียหี", "เลียแตด",

            // 4. Severe curse words & animal insults
            "เหี้ย", "ไอ้เหี้ย", "อีเหี้ย", "ไอ่เหี้ย", "เชี่ย", "ไอ้เชี่ย", "อีเชี่ย",
            "เห้", "ไอ้เห้", "อีเห้", "ไอ่เห้", "หี้ย",
            "สัส", "สัด", "สึด", "สาส", "สาซ", "ไอ้สัส", "อีสัส", "ไอ้สัตว์", "อีสัตว์",
            "ดอกทอง", "อีดอก", "อีดอกทอง", "ชาติหมา", "ไอ้ชาติหมา", "อีชาติหมา",
            "จัญไร", "อัปปรีย์", "ระยำ", "สารเลว", "สถุล", "สันดาน", "หน้าด้าน",
            "ตอแหล", "บัดซบ", "เสือก", "ส้นตีน", "กวนตีน", "ไอ้ควาย", "อีควาย",
            "แม่ง", "มึง", "กู", "ห่า", "ไอ้ห่า", "อีห่า", "เปรต", "ไอ้เวร", "อีเวร",
            "ลูกกะหรี่", "ลูกโสเภณี", "ลูกอีช่อ", "ลูกไม่มีพ่อ", "เด็กเปรต",
            "โคตรพ่อ", "โคตรแม่", "ยัดแม่", "ชิบหาย", "ฉิบหาย",
            "หน้าตัวเมีย", "ขยะสังคม",

            // 5. Shortened / Missing letter Thai gaming slang
            "คย", "ค.ย.", "พมต", "มมต", "พ่องตาย", "แม่งตาย", "หาพ่อง", "พ่อง",

            // 6. Threats, death wishes, and violence (คำข่มขู่ เอาชีวิต และทำร้าย)
            "ไปตาย", "ไปตายซะ", "ไปผูกคอตาย", "ฆ่ามึง", "กูจะฆ่ามึง", "ยิงมึง", "แทงมึง",
            "ปาดคอ", "ฆ่าตัวตาย", "ตายโหง", "ตายห่า", "ตายซะ", "นอนโลง",
            "พ่อมึงตาย", "แม่มึงตาย", "ล้างโคตร", "ตายทั้งโคตร",

            // 7. Assault, sexual abuse, molestation (การล่วงละเมิดทางเพศ ทำร้าย คุกคาม)
            "ข่มขืน", "รุมโทรม", "ลวนลาม", "อนาจาร", "จับนม", "ล้วงหี", "ลวนลามเด็ก", "ใคร่เด็ก",

            // 8. Hate speech, ableism, dehumanizing slurs (คำเหยียด สร้างความเกลียดชัง)
            "ปัญญาอ่อน", "ไอ้ปัญญาอ่อน", "อีปัญญาอ่อน",
            "ไอ้เอ๋อ", "อีเอ๋อ", "บ้ากาม", "โรคจิต", "ไอ้โรคจิต", "อีโรคจิต", "คนบ้า",
            "ไอ้บ้า", "อีบ้า", "ไอ้สวะ", "อีสวะ", "เศษสวะ",

            // 9. English inappropriate words written in Thai phonetic script (คำหยาบภาษาอังกฤษเขียนไทย)
            "ฟัค", "ฟัก", "ฟักยู", "ฟัคยู", "ฟักกิ้ง", "ฟัคกิ้ง", "ฟักเกอร์", "ฟัคเกอร์",
            "ชิท", "บิทช์", "บิช", "บิตช์",
            "พอร์น", "เซ็กส์", "เซกส์", "เซ็ก", "เซก", "เซ็กซี่",
            "นู้ด", "ดิก", "ดิ๊ก", "ค็อก",
            "พุสซี่", "พุซซี่", "แอสโฮล", "แอสโซล", "เควายเอส",
            "ดิลโด้", "คัม", "ฮอร์นี่", "สลัท", "เรป"
        };

        // Phonetic / karaoke Thai slang terms
        private static readonly string[] ThaiKaraokeProhibitedTerms =
        {
            "kuy", "kuay", "kwai", "kway", "kooy",
            "hum", "huum", "ham",
            "hee", "heee", "hiii",
            "sus", "suss", "sat", "sud", "sut", "sas",
            "yed", "yet", "yedd", "yett",
            "hia", "hiea", "heea", "chia", "cheea",
            "ted", "taed",
            "jim", "jimm", "chim",
            "garee", "karee", "karhee", "gari", "kari",
            "mung", "meung", "mueng",
            "goo", "guu",
            "dorkthong", "dokthong"
        };

        // Prohibited multi-word and phonetic innuendo compounds (e.g., "YesMOM" phonetic for "เย็ดแม่")
        private static readonly string[] ProhibitedCompoundPhrases =
        {
            // Yes-X Thai innuendo compounds
            "yesmom", "yesmae", "yesmother", "yesmama", "yespor", "yesdad", "yesfather",
            "yesped", "yespet", "yeskae", "yeskhe", "yesmeung", "yesmung",

            // Yed-X compounds
            "yedmom", "yedmae", "yedmother", "yedpor", "yedpet", "yedped", "yedkhe", "yedkae",
            "yetmom", "yetmae", "yetpor", "yetped",

            // Fuck-X family compounds
            "fuckmom", "fuckmae", "fuckmother", "fuckdad", "fuckpor", "fucku", "fcku", "fku",
            "fuckoff", "fkoff", "fckoff", "gofuckyourself",

            // Threats, self-harm, hate compounds
            "killyourself", "killurself", "killyou", "godie", "dieinafire",
            "slitthroat", "hangyourself", "hateyou", "ihateyou",

            // English sexual/insult compound phrases
            "eatshit", "suckdick", "sucdick", "suckmydick", "sucmydic", "suckcock", "eatdick",

            // Karaoke death threats / toxic parental insults
            "pormungtai", "maemungtai", "porteungtai", "maeteungtai", "kuyrai", "kuyyrai"
        };

        private static readonly string[] ProhibitedThaiCompoundPhrases =
        {
            "เยสมัม", "เยสแม่", "เยสพ่อ", "เยสเป็ด", "เยสเข้", "เยสม้า", "เยสมึง",
            "เย็ดมัม", "เยดมัม", "เย็ดแม่", "เย็ดพ่อ", "เย็ดเป็ด", "เย็ดเข้",
            "ฟัคยู", "ฟักยู", "ฟัคกิ้ง", "ฟักกิ้ง", "ฟัคเกอร์", "ฟักเกอร์",
            "เลียหี", "ดูดควย", "เลียแตด", "อมควย", "อมนกเขา",
            "พ่อมึงตาย", "แม่มึงตาย", "พ่องตาย", "แม่งตาย", "โคตรพ่อ", "โคตรแม่",
            "ไปตายซะ", "ไปผูกคอตาย", "กูจะฆ่ามึง", "ฆ่าตัวตาย",
            "ข่มขืน", "รุมโทรม", "ลวนลามเด็ก", "ใคร่เด็ก",
            "ปัญญาอ่อน", "ไอ้ปัญญาอ่อน", "อีปัญญาอ่อน", "ไอ้เอ๋อ", "อีเอ๋อ",
            "ขยะสังคม", "เศษสวะ"
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

            // 4. Scunthorpe / Allowlist check (exact match on safe name)
            if (ScunthorpeAllowList.Contains(normalizedName))
            {
                result = DisplayNameValidationResult.Success();
                return true;
            }

            // 5. Kid-Friendly Appropriateness Filter (English & Thai)
            if (CheckInappropriateContent(normalizedName, out string flagged))
            {
                AppLog.Warning("DisplayNamePolicy", $"Name '{normalizedName}' denied: Prohibited word detected ('{flagged}').");
                result = DisplayNameValidationResult.Denied(
                    DisplayNameDenialReason.InappropriateContent,
                    "onboarding.nameInappropriate",
                    flagged);
                return false;
            }

            // 6. Contact / PII protection (Roblox-grade COPPA child safety)
            if (ContactPhoneRegex.IsMatch(normalizedName) ||
                ContactLinkRegex.IsMatch(normalizedName) ||
                SocialHandleRegex.IsMatch(normalizedName))
            {
                AppLog.Warning("DisplayNamePolicy", $"Name '{normalizedName}' denied: Contains contact or PII pattern.");
                result = DisplayNameValidationResult.Denied(
                    DisplayNameDenialReason.ContactInformation,
                    "onboarding.nameContactInfo");
                return false;
            }

            result = DisplayNameValidationResult.Success();
            return true;
        }

        private static bool CheckInappropriateContent(string input, out string flaggedTerm)
        {
            flaggedTerm = null;

            // 1. Thai compound phrases & innuendos (raw, delimiter-stripped, collapsed repeats)
            string strippedThai = CanonicalizeThai(input);
            string collapsedThai = ConsecutiveRepeatsRegex.Replace(strippedThai, "$1");

            foreach (string compound in ProhibitedThaiCompoundPhrases)
            {
                if (input.IndexOf(compound, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    strippedThai.IndexOf(compound, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    collapsedThai.IndexOf(compound, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    flaggedTerm = compound;
                    return true;
                }
            }

            // 2. Normalized Latin string & Latin compound phrases (e.g., "YesMOM", "YesMae", "EatShit")
            string rawDelimStripped = StripDelimitersOnly(input);
            string rawCollapsed = ConsecutiveRepeatsRegex.Replace(rawDelimStripped, "$1");
            string normalizedLatin = CanonicalizeForFilter(input);
            string collapsedLatin = ConsecutiveRepeatsRegex.Replace(normalizedLatin, "$1$1");
            string singleCollapsedLatin = ConsecutiveRepeatsRegex.Replace(normalizedLatin, "$1");

            foreach (string compound in ProhibitedCompoundPhrases)
            {
                if (rawDelimStripped.IndexOf(compound, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    rawCollapsed.IndexOf(compound, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    normalizedLatin.IndexOf(compound, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    collapsedLatin.IndexOf(compound, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    singleCollapsedLatin.IndexOf(compound, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    flaggedTerm = compound;
                    return true;
                }
            }

            // 3. Thai prohibited terms against raw input, delimiter-stripped Thai, and collapsed repeats
            foreach (string thaiTerm in ThaiProhibitedTerms)
            {
                if (MatchesThaiTerm(input, strippedThai, collapsedThai, thaiTerm))
                {
                    flaggedTerm = thaiTerm;
                    return true;
                }
            }

            // 4. English prohibited terms
            foreach (string term in EnglishProhibitedTerms)
            {
                if (MatchesTerm(rawDelimStripped, input, term) ||
                    MatchesTerm(rawCollapsed, input, term) ||
                    MatchesTerm(normalizedLatin, input, term) ||
                    MatchesTerm(collapsedLatin, input, term) ||
                    MatchesTerm(singleCollapsedLatin, input, term))
                {
                    flaggedTerm = term;
                    return true;
                }
            }

            // 5. Thai Karaoke terms (e.g. "kuy", "kuay", "hum", "hee", "sus", "yed")
            foreach (string term in ThaiKaraokeProhibitedTerms)
            {
                if (MatchesTerm(rawDelimStripped, input, term) ||
                    MatchesTerm(rawCollapsed, input, term) ||
                    MatchesTerm(normalizedLatin, input, term) ||
                    MatchesTerm(collapsedLatin, input, term) ||
                    MatchesTerm(singleCollapsedLatin, input, term))
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
            else if (thaiTerm == "ฟัก")
            {
                if (rawInput.IndexOf("ฟักทอง", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    rawInput.IndexOf("ต้มฟัก", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    rawInput.IndexOf("แกงฟัก", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    rawInput.IndexOf("ฟักเขียว", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    rawInput.IndexOf("ฟักไข่", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string clean = strippedThai
                        .Replace("ฟักทอง", "")
                        .Replace("ต้มฟัก", "")
                        .Replace("แกงฟัก", "")
                        .Replace("ฟักเขียว", "")
                        .Replace("ฟักไข่", "");
                    if (clean.IndexOf("ฟัก", StringComparison.OrdinalIgnoreCase) < 0)
                        return false;
                }
            }
            else if (thaiTerm == "คัม")
            {
                if (rawInput.IndexOf("คัมภีร์", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    rawInput.IndexOf("คัมภีร์เวท", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string clean = strippedThai.Replace("คัมภีร์เวท", "").Replace("คัมภีร์", "");
                    if (clean.IndexOf("คัม", StringComparison.OrdinalIgnoreCase) < 0)
                        return false;
                }
            }
            else if (thaiTerm == "ดิก")
            {
                if (rawInput.IndexOf("ดิกชันนารี", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string clean = strippedThai.Replace("ดิกชันนารี", "");
                    if (clean.IndexOf("ดิก", StringComparison.OrdinalIgnoreCase) < 0)
                        return false;
                }
            }
            else if (thaiTerm == "ค็อก")
            {
                if (rawInput.IndexOf("ค็อกเทล", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    rawInput.IndexOf("ค็อกพิท", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string clean = strippedThai.Replace("ค็อกเทล", "").Replace("ค็อกพิท", "");
                    if (clean.IndexOf("ค็อก", StringComparison.OrdinalIgnoreCase) < 0)
                        return false;
                }
            }
            else if (thaiTerm == "คย")
            {
                // Universal Thai abbreviation for "ควย"
                if (rawInput.IndexOf("เทคนิค", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    rawInput.IndexOf("ภาคยานุวัติ", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string clean = strippedThai.Replace("เทคนิค", "").Replace("ภาคยานุวัติ", "");
                    if (clean.IndexOf("คย", StringComparison.OrdinalIgnoreCase) < 0)
                        return false;
                }
            }

            return true;
        }

        private static bool MatchesTerm(string searchTarget, string originalInput, string prohibitedTerm)
        {
            if (string.IsNullOrEmpty(searchTarget) || string.IsNullOrEmpty(prohibitedTerm)) return false;

            int index = 0;
            while ((index = searchTarget.IndexOf(prohibitedTerm, index, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                bool isMatch = true;

                // For short terms (<= 3 chars like sex, ass, tit, hum, hee, sus, kuy, yed, fuc, suc, fck, dic, dik, bch, cnt, kys),
                // or specific transliterations
                if (prohibitedTerm.Length <= 3 || prohibitedTerm == "garee" || prohibitedTerm == "karee")
                {
                    // Terms that have NO legitimate English words can match unconditionally unless in allowlist
                    if (prohibitedTerm == "kuy" || prohibitedTerm == "kuay" || prohibitedTerm == "fck" ||
                        prohibitedTerm == "cnt" || prohibitedTerm == "bch" || prohibitedTerm == "kys" ||
                        prohibitedTerm == "stfu" || prohibitedTerm == "gtfo")
                    {
                        foreach (string safeWord in ScunthorpeAllowList)
                        {
                            if (originalInput.IndexOf(safeWord, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                isMatch = false;
                                break;
                            }
                        }

                        if (isMatch) return true;
                    }
                    else
                    {
                        // Require letter boundaries: not immediately preceded or followed by an English letter
                        bool startBound = index == 0 || !char.IsLetter(searchTarget[index - 1]);
                        bool endBound = (index + prohibitedTerm.Length >= searchTarget.Length) ||
                                        !char.IsLetter(searchTarget[index + prohibitedTerm.Length]);

                        if (startBound && endBound)
                        {
                            foreach (string safeWord in ScunthorpeAllowList)
                            {
                                if (originalInput.IndexOf(safeWord, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    isMatch = false;
                                    break;
                                }
                            }

                            if (isMatch) return true;
                        }
                    }
                }
                else
                {
                    // For longer words (>= 4 chars like fuck, shit, bitch, porn, dokthong),
                    // check if embedded in an allowlisted safe word
                    foreach (string safeWord in ScunthorpeAllowList)
                    {
                        if (originalInput.IndexOf(safeWord, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            isMatch = false;
                            break;
                        }
                    }

                    if (isMatch) return true;
                }

                index += prohibitedTerm.Length;
            }

            return false;
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
                    case '¢':
                    case '©':
                        sb.Append('c');
                        break;
                    case '3':
                    case '€':
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
                    case '§':
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

        /// <summary>
        /// Strips common delimiters and whitespace without performing leetspeak character substitutions.
        /// Useful for boundary-checking terms against numbers and punctuation (e.g. "hee555", "sus123").
        /// </summary>
        public static string StripDelimitersOnly(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var sb = new StringBuilder(text.Length);
            foreach (char rawChar in text)
            {
                if (rawChar == '\u200B' || rawChar == '\u200C' || rawChar == '\u200D' || rawChar == '\uFEFF')
                    continue;

                if (char.IsWhiteSpace(rawChar) || rawChar == '.' || rawChar == '_' || rawChar == '-' ||
                    rawChar == '/' || rawChar == '\\' || rawChar == '*' || rawChar == '~' ||
                    rawChar == '^' || rawChar == '|' || rawChar == '+' || rawChar == ':' ||
                    rawChar == ';' || rawChar == ',' || rawChar == '?' || rawChar == '%' ||
                    rawChar == '&' || rawChar == '(' || rawChar == ')' || rawChar == '[' ||
                    rawChar == ']' || rawChar == '{' || rawChar == '}')
                    continue;

                sb.Append(char.ToLowerInvariant(rawChar));
            }

            return sb.ToString();
        }
    }
}
