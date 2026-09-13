using System;

namespace PowerMath.Gameplay.Academic.Core
{
    public static class FallbackQuestionEquationGenerator
    {
        /// <summary>
        /// Generates a clean arithmetic equation matching the target answer.
        /// </summary>
        public static string FormatEquation(int answer, int questionId)
        {
            if (answer == 8)
            {
                return "48 ÷ 6 = ?";
            }

            if (answer <= 0)
            {
                return "0 + 0 = ?";
            }

            switch (Math.Abs(questionId) % 4)
            {
                case 1:
                {
                    int divisor = (Math.Abs(questionId) % 5) + 2;
                    return $"{answer * divisor} ÷ {divisor} = ?";
                }
                case 2:
                {
                    int addend = Math.Max(1, Math.Min(answer - 1, (answer / 3) + 1));
                    int first = answer - addend;
                    return $"{first} + {addend} = ?";
                }
                case 3:
                {
                    for (int factor = 9; factor >= 2; factor--)
                    {
                        if (answer % factor == 0 && answer / factor <= 12)
                        {
                            return $"{factor} × {answer / factor} = ?";
                        }
                    }

                    int subtrahend = (Math.Abs(questionId) % 10) + 2;
                    return $"{answer + subtrahend} - {subtrahend} = ?";
                }
                default:
                {
                    int subtrahend = (Math.Abs(questionId) % 8) + 5;
                    return $"{answer + subtrahend} - {subtrahend} = ?";
                }
            }
        }
    }
}
