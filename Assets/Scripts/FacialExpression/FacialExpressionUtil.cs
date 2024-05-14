using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.OpenXR.Features.MagicLeapSupport;

public class FacialExpressionUtil
{
    public static readonly Dictionary<MagicLeapFacialExpressionFeature.FacialBlendShape, string> FacialBlendShapes
        = new Dictionary<MagicLeapFacialExpressionFeature.FacialBlendShape, string>()
    {
        { MagicLeapFacialExpressionFeature.FacialBlendShape.BrowLowererL, "Left Brow Lowerer" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.BrowLowererR, "Right Brow Lowerer "},
        { MagicLeapFacialExpressionFeature.FacialBlendShape.CheekRaiserL, "Left Cheek Raiser" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.CheekRaiserR, "Right Cheek Raiser" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.ChinRaiser, "Chin Raiser" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.DimplerL, "Left Dimpler" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.DimplerR, "Right Dimpler" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.EyesClosedL, "Left Eye Closed" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.EyesClosedR, "Right Eye Closed" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.InnerBrowRaiserL, "Left Inner Brow Raiser" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.InnerBrowRaiserR, "Right Inner Brow Raiser" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.JawDrop, "Jaw Drop" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.LidTightenerL, "Left Lid Tightener" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.LidTightenerR, "Right Lid Tightener" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.LipCornerDepressorL, "Left Lip Corner Depressor" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.LipCornerDepressorR, "Right Lip Corner Depressor" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.LipCornerPullerL, "Left Corner Puller" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.LipCornerPullerR, "Right Corner Puller" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.LipFunnelerLB, "Left Bottom Lip Funneler" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.LipFunnelerLT, "Left Top Lip Funneler" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.LipFunnelerRB, "Right Bottom Lip Funneler" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.LipFunnelerRT, "Right Top Lip Funneler" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.LipPressorL, "Left Lip Pressor" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.LipPressorR, "Right Lip Pressor" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.LipPuckerL, "Left Lip Pucker" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.LipPuckerR, "Right Lip Pucker" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.LipStretcherL, "Left Lip Stretcher" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.LipStretcherR, "Right Lip Stretcher" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.LipSuckLB, "Left Bottom Lip Suck" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.LipSuckLT, "Left Top Lip Suck" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.LipSuckRB, "Right Bottom Lip Suck" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.LipSuckRT, "Right Top Lip Suck" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.LipTightenerL, "Left Lip Tightener" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.LipTightenerR, "Right Lip Tightener" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.LipsToward, "Lips Toward" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.LowerLipDepressorL, "Left Lower Lip Depressor" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.LowerLipDepressorR, "Right Lower Lip Depressor" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.NoseWrinklerL, "Left Nose Wrinkler" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.NoseWrinklerR, "Right Nose Wrinkler" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.OuterBrowRaiserL, "Left Outer Brow Raiser" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.OuterBrowRaiserR, "Right Outer Brow Raiser" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.UpperLidRaiserL, "Left Upper Lid Raiser" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.UpperLidRaiserR, "Right Upper Lid Raiser" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.UpperLipRaiserL, "Left Upper Lip Raiser" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.UpperLipRaiserR, "Right Upper Lip Raiser" },
        { MagicLeapFacialExpressionFeature.FacialBlendShape.TongueOut, "Tongue Out" }
    };
}
