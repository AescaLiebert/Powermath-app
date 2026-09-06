using System;
using UnityEngine.UIElements;

namespace PowerMath.UI.Core
{
    /// <summary>Compatibility alias. New UXML should use FigmaGradientElement.</summary>
    [Obsolete("Use FigmaGradientElement with USS tokens instead.")]
    [UxmlElement]
    public sealed partial class BossHpGradientFill : FigmaGradientElement
    {
    }
}
