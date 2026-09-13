using System;
using System.Collections.Generic;
using NUnit.Framework;
using PowerMath.Diagnostics;
using PowerMath.Localization;
using PowerMath.Session;
using PowerMath.UI.Core;

namespace PowerMath.Tests.EditMode
{
    public sealed class DisplayNamePolicyTests
    {
        [Test]
        public void EmptyOrWhitespace_IsDenied()
        {
            Assert.IsFalse(DisplayNamePolicy.TryValidate("", out _, out var resEmpty));
            Assert.AreEqual(DisplayNameDenialReason.EmptyOrWhitespace, resEmpty.Reason);

            Assert.IsFalse(DisplayNamePolicy.TryValidate("   \t  ", out _, out var resWhitespace));
            Assert.AreEqual(DisplayNameDenialReason.EmptyOrWhitespace, resWhitespace.Reason);
        }

        [Test]
        public void LengthBoundary_EnforcesTwentyCharacters()
        {
            // 20 characters is allowed
            string exact20 = "12345678901234567890";
            Assert.IsTrue(DisplayNamePolicy.TryValidate(exact20, out string norm20, out var res20));
            Assert.AreEqual(DisplayNameDenialReason.None, res20.Reason);
            Assert.AreEqual(exact20, norm20);

            // 21 characters is rejected
            string tooLong = "123456789012345678901";
            Assert.IsFalse(DisplayNamePolicy.TryValidate(tooLong, out _, out var resLong));
            Assert.AreEqual(DisplayNameDenialReason.TooLong, resLong.Reason);
            Assert.AreEqual("onboarding.nameTooLong", resLong.LocalizationKey);
        }

        [Test]
        public void ThaiLanguageGraphemes_CountCorrectly()
        {
            // "ผู้กล้าแห่งสายลม" has Thai vowels and tone marks, well within 20 text elements
            string thaiName = "ผู้กล้าแห่งสายลม";
            Assert.IsTrue(DisplayNamePolicy.TryValidate(thaiName, out string normThai, out var resThai));
            Assert.AreEqual(DisplayNameDenialReason.None, resThai.Reason);
            Assert.AreEqual(thaiName, normThai);
        }

        [TestCase("fuck")]
        [TestCase("shit")]
        [TestCase("bitch")]
        [TestCase("cunt")]
        [TestCase("porn")]
        [TestCase("nude")]
        [TestCase("asshole")]
        [TestCase("dick")]
        public void EnglishProfanityAndNudity_DirectTerms_AreDenied(string input)
        {
            Assert.IsFalse(DisplayNamePolicy.TryValidate(input, out _, out var result));
            Assert.AreEqual(DisplayNameDenialReason.InappropriateContent, result.Reason);
            Assert.AreEqual("onboarding.nameInappropriate", result.LocalizationKey);
        }

        [TestCase("f.u.c.k")]
        [TestCase("s_h_i_t")]
        [TestCase("b!tch")]
        [TestCase("p0rn")]
        [TestCase("@sshole")]
        [TestCase("fuuuuck")]
        [TestCase("shiiit")]
        public void EnglishProfanity_ObfuscatedAndLeetspeak_AreDenied(string input)
        {
            Assert.IsFalse(DisplayNamePolicy.TryValidate(input, out _, out var result));
            Assert.AreEqual(DisplayNameDenialReason.InappropriateContent, result.Reason);
            Assert.AreEqual("onboarding.nameInappropriate", result.LocalizationKey);
        }

        [TestCase("ควย")]
        [TestCase("หี")]
        [TestCase("ควย / หี")]
        [TestCase("ค ว ย")]
        [TestCase("ค-ว-ย")]
        [TestCase("ค.ว.ย")]
        [TestCase("ห ี")]
        [TestCase("ห/ี")]
        [TestCase("ไอ้เหี้ย")]
        [TestCase("ไอ่เหี้ย")]
        [TestCase("ไอ้ เหี้ย")]
        [TestCase("เชี่ย")]
        [TestCase("สัส")]
        [TestCase("สั ส")]
        [TestCase("สัสสส")]
        [TestCase("มึงสาด")]
        [TestCase("เย็ด")]
        [TestCase("ดอกทอง")]
        [TestCase("ชาติหมา")]
        [TestCase("kuy")]
        [TestCase("hiea")]
        [TestCase("hee")]
        public void ThaiProfanityAndToxicWords_AreDenied(string input)
        {
            Assert.IsFalse(DisplayNamePolicy.TryValidate(input, out _, out var result),
                $"Offensive word '{input}' should have been denied.");
            Assert.AreEqual(DisplayNameDenialReason.InappropriateContent, result.Reason);
            Assert.AreEqual("onboarding.nameInappropriate", result.LocalizationKey);
        }

        [TestCase("Hum")]
        [TestCase("hum99")]
        [TestCase("Hee")]
        [TestCase("hee555")]
        [TestCase("Sus")]
        [TestCase("sus123")]
        [TestCase("Kuy")]
        [TestCase("kuyza")]
        [TestCase("Kuay")]
        [TestCase("Yed")]
        [TestCase("yed99")]
        [TestCase("Hia")]
        [TestCase("Dokthong")]
        [TestCase("Dorkthong")]
        [TestCase("Meung")]
        public void ThaiProfanityWrittenInEnglish_AreDenied(string input)
        {
            Assert.IsFalse(DisplayNamePolicy.TryValidate(input, out _, out var result),
                $"Thai-in-EN word '{input}' should have been denied.");
            Assert.AreEqual(DisplayNameDenialReason.InappropriateContent, result.Reason);
            Assert.AreEqual("onboarding.nameInappropriate", result.LocalizationKey);
        }

        [TestCase("ฟัค")]
        [TestCase("ฟักยู")]
        [TestCase("ฟัคยู")]
        [TestCase("ชิท")]
        [TestCase("บิทช์")]
        [TestCase("พอร์น")]
        [TestCase("เซ็กส์")]
        [TestCase("นู้ด")]
        [TestCase("ดิก")]
        [TestCase("ค็อก")]
        public void EnglishProfanityWrittenInThaiScript_AreDenied(string input)
        {
            Assert.IsFalse(DisplayNamePolicy.TryValidate(input, out _, out var result),
                $"EN-in-TH word '{input}' should have been denied.");
            Assert.AreEqual(DisplayNameDenialReason.InappropriateContent, result.Reason);
            Assert.AreEqual("onboarding.nameInappropriate", result.LocalizationKey);
        }

        [TestCase("YesMom")]
        [TestCase("Yes-Mom")]
        [TestCase("Yes_Mom")]
        [TestCase("yes.mom")]
        [TestCase("YesMae")]
        [TestCase("YesMother")]
        [TestCase("เยสมัม")]
        [TestCase("เยส-มัม")]
        [TestCase("เยสแม่")]
        [TestCase("YedMom")]
        [TestCase("EatShit")]
        [TestCase("SuckDick")]
        [TestCase("พ่อมึงตาย")]
        public void CompoundInnuendosAndPhrases_AreDenied(string input)
        {
            Assert.IsFalse(DisplayNamePolicy.TryValidate(input, out _, out var result),
                $"Compound innuendo '{input}' should have been denied.");
            Assert.AreEqual(DisplayNameDenialReason.InappropriateContent, result.Reason);
            Assert.AreEqual("onboarding.nameInappropriate", result.LocalizationKey);
        }

        [TestCase("fuc")]
        [TestCase("fck")]
        [TestCase("fuk")]
        [TestCase("dic")]
        [TestCase("dik")]
        [TestCase("suc")]
        [TestCase("sux")]
        [TestCase("bch")]
        [TestCase("cnt")]
        [TestCase("stfu")]
        [TestCase("gtfo")]
        [TestCase("wtf")]
        [TestCase("kys")]
        public void ShortenedAndAbbreviatedToxicWords_AreDenied(string input)
        {
            Assert.IsFalse(DisplayNamePolicy.TryValidate(input, out _, out var result),
                $"Shortened toxic word '{input}' should have been denied.");
            Assert.AreEqual(DisplayNameDenialReason.InappropriateContent, result.Reason);
            Assert.AreEqual("onboarding.nameInappropriate", result.LocalizationKey);
        }

        [TestCase("A$$")]
        [TestCase("A$s")]
        [TestCase("a$$hole")]
        [TestCase("fu¢k")]
        [TestCase("b!tch")]
        [TestCase("d!ck")]
        [TestCase("5h!t")]
        [TestCase("p0rn")]
        public void SpecialSymbolsAndLeetspeakVariations_AreDenied(string input)
        {
            Assert.IsFalse(DisplayNamePolicy.TryValidate(input, out _, out var result),
                $"Symbol obfuscation '{input}' should have been denied.");
            Assert.AreEqual(DisplayNameDenialReason.InappropriateContent, result.Reason);
            Assert.AreEqual("onboarding.nameInappropriate", result.LocalizationKey);
        }

        [TestCase("kill yourself")]
        [TestCase("go die")]
        [TestCase("kill you")]
        [TestCase("suicide")]
        [TestCase("rape")]
        [TestCase("molest")]
        [TestCase("nigger")]
        [TestCase("faggot")]
        [TestCase("retard")]
        public void ThreatsViolenceAndHateSpeechEnglish_AreDenied(string input)
        {
            Assert.IsFalse(DisplayNamePolicy.TryValidate(input, out _, out var result),
                $"Hate/threat term '{input}' should have been denied.");
            Assert.AreEqual(DisplayNameDenialReason.InappropriateContent, result.Reason);
            Assert.AreEqual("onboarding.nameInappropriate", result.LocalizationKey);
        }

        [TestCase("คย")]
        [TestCase("ค.ย.")]
        [TestCase("ค-ย")]
        [TestCase("ค_ย")]
        [TestCase("เห้")]
        [TestCase("ไอ้เห้")]
        [TestCase("พมต")]
        [TestCase("มมต")]
        [TestCase("พ่องตาย")]
        [TestCase("แม่งตาย")]
        [TestCase("หาพ่อง")]
        public void ThaiShortenedAndGamingSlang_AreDenied(string input)
        {
            Assert.IsFalse(DisplayNamePolicy.TryValidate(input, out _, out var result),
                $"Thai slang '{input}' should have been denied.");
            Assert.AreEqual(DisplayNameDenialReason.InappropriateContent, result.Reason);
            Assert.AreEqual("onboarding.nameInappropriate", result.LocalizationKey);
        }

        [TestCase("ไปตายซะ")]
        [TestCase("กูจะฆ่ามึง")]
        [TestCase("ฆ่าตัวตาย")]
        [TestCase("ข่มขืน")]
        [TestCase("รุมโทรม")]
        [TestCase("ปัญญาอ่อน")]
        [TestCase("ไอ้ปัญญาอ่อน")]
        [TestCase("ไอ้เอ๋อ")]
        [TestCase("เศษสวะ")]
        public void ThaiThreatsViolenceAndHateSpeech_AreDenied(string input)
        {
            Assert.IsFalse(DisplayNamePolicy.TryValidate(input, out _, out var result),
                $"Thai threat/hate term '{input}' should have been denied.");
            Assert.AreEqual(DisplayNameDenialReason.InappropriateContent, result.Reason);
            Assert.AreEqual("onboarding.nameInappropriate", result.LocalizationKey);
        }

        [TestCase("Pass")]
        [TestCase("Classic")]
        [TestCase("Assistant")]
        [TestCase("Butter")]
        [TestCase("Grass")]
        [TestCase("Titan")]
        [TestCase("Hello")]
        [TestCase("Ricko")]
        [TestCase("Stellar")]
        [TestCase("Humphrey")]
        [TestCase("Sustain")]
        [TestCase("Grape")]
        [TestCase("Grapes")]
        [TestCase("Success")]
        [TestCase("Dictionary")]
        [TestCase("Fuchsia")]
        [TestCase("Asset")]
        [TestCase("Assemble")]
        [TestCase("ผู้กล้า")]
        [TestCase("หีบ")]
        [TestCase("หีบสมบัติ")]
        [TestCase("กูเกิล")]
        [TestCase("ฟักทอง")]
        [TestCase("คัมภีร์")]
        [TestCase("ดิกชันนารี")]
        [TestCase("ค็อกเทล")]
        [TestCase("เทคนิค")]
        [TestCase("ผู้ฆ่ามังกร")]
        [TestCase("ปัญญา")]
        public void ScunthorpeAllowlist_PermitsHarmlessWords(string input)
        {
            Assert.IsTrue(DisplayNamePolicy.TryValidate(input, out string normalized, out var result),
                $"Safe word '{input}' should not be blocked by Scunthorpe filter.");
            Assert.AreEqual(DisplayNameDenialReason.None, result.Reason);
            Assert.AreEqual(input, normalized);
        }

        [TestCase("081-234-5678")]
        [TestCase("0812345678")]
        [TestCase("095-888-9999")]
        [TestCase("visit game.com")]
        [TestCase("discord.gg/play")]
        [TestCase("http://badsite")]
        [TestCase("@gmail")]
        [TestCase("@twitter")]
        [TestCase("user@gmail.com")]
        [TestCase("@somchai")]
        [TestCase("ig:somchai")]
        [TestCase("fb:alex")]
        [TestCase("line:student01")]
        [TestCase("dc:gamer#1234")]
        public void ContactInformationAndUrls_AreDenied(string input)
        {
            Assert.IsFalse(DisplayNamePolicy.TryValidate(input, out _, out var result),
                $"Contact/PII '{input}' should have been denied.");
            Assert.AreEqual(DisplayNameDenialReason.ContactInformation, result.Reason);
            Assert.AreEqual("onboarding.nameContactInfo", result.LocalizationKey);
        }

        [Test]
        public void StatusMessageService_EmitsWarningWhenDenied()
        {
            string publishedMessage = null;
            StatusSeverity publishedSeverity = StatusSeverity.Info;

            Action<string, StatusSeverity, int> handler = (msg, sev, dur) =>
            {
                publishedMessage = msg;
                publishedSeverity = sev;
            };

            StatusMessageService.MessagePublished += handler;
            try
            {
                bool valid = PlayerLifecyclePolicy.TryNormalizeName("bad_word_fuck", out _, out var result);
                Assert.IsFalse(valid);
                Assert.AreEqual(DisplayNameDenialReason.InappropriateContent, result.Reason);

                string localized = LocalizationService.Get(result.LocalizationKey);
                StatusMessageService.ShowWarning(localized);

                Assert.IsNotNull(publishedMessage);
                Assert.AreEqual(StatusSeverity.Warning, publishedSeverity);
                Assert.AreEqual(localized, publishedMessage);
            }
            finally
            {
                StatusMessageService.MessagePublished -= handler;
            }
        }

        [TestCase("level1:student01", "student01")]
        [TestCase("grade2:somchai99", "somchai99")]
        public void AccountUsernameExtraction_ResolvesUsernameCorrectly(string playerId, string expected)
        {
            var snapshot = new PowerMath.PlayerData.PlayerSnapshot { playerId = playerId };
            bool ok = PowerMath.UI.Settings.AdminAccountAccessPolicy.TryGetUsername(snapshot, out string username);
            Assert.IsTrue(ok);
            Assert.AreEqual(expected, username);
        }
    }
}
