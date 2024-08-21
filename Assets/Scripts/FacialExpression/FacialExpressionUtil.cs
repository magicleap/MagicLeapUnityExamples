// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2024) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MagicLeap.OpenXR.Features.FacialExpressions;

public class FacialExpressionUtil
{
    public const int LipSuckB = -1;
    public const int LipSuckT = -2;
    public const int LipTightener = -3;
    
    public static readonly Dictionary<int, int> FacialModelBlendShapes
        = new Dictionary<int, int>()
    {
        // swap original L and R values to allow mirroring effect when looking at model
        { 0, (int)FacialBlendShape.BrowLowererR },
        { 1, (int)FacialBlendShape.BrowLowererL },
        { 2, (int)FacialBlendShape.CheekRaiserR },
        { 3, (int)FacialBlendShape.CheekRaiserL },
        { 4, (int)FacialBlendShape.ChinRaiser },
        { 5, (int)FacialBlendShape.DimplerR },
        { 6, (int)FacialBlendShape.DimplerL },
        { 7, (int)FacialBlendShape.EyesClosedR },
        { 8, (int)FacialBlendShape.EyesClosedL },
        { 9, (int)FacialBlendShape.InnerBrowRaiserR },
        { 10, (int)FacialBlendShape.InnerBrowRaiserL },
        { 11, (int)FacialBlendShape.JawDrop },
        { 12, (int)FacialBlendShape.LidTightenerR },
        { 13, (int)FacialBlendShape.LidTightenerL },
        { 14, (int)FacialBlendShape.LipCornerDepressorR },
        { 15, (int)FacialBlendShape.LipCornerDepressorL },
        { 16, (int)FacialBlendShape.LipCornerPullerR },
        { 17, (int)FacialBlendShape.LipCornerPullerL },
        { 18, (int)FacialBlendShape.LipFunnelerRB },
        { 19, (int)FacialBlendShape.LipFunnelerRT },
        { 20, (int)FacialBlendShape.LipFunnelerLB },
        { 21, (int)FacialBlendShape.LipFunnelerLT },
        { 22, (int)FacialBlendShape.LipPressorR },
        { 23, (int)FacialBlendShape.LipPressorL },
        { 24, (int)FacialBlendShape.LipPuckerR },
        { 25, (int)FacialBlendShape.LipPuckerL },
        { 26, (int)FacialBlendShape.LipStretcherR },
        { 27, (int)FacialBlendShape.LipStretcherL },
        { 28, LipSuckB },
        { 29, LipSuckT },
        { 30, LipTightener },
        { 31, (int)FacialBlendShape.LipsToward },
        { 32, (int)FacialBlendShape.LowerLipDepressorR },
        { 33, (int)FacialBlendShape.LowerLipDepressorL },
        { 34, (int)FacialBlendShape.NoseWrinklerR },
        { 35, (int)FacialBlendShape.NoseWrinklerL },
        { 36, (int)FacialBlendShape.OuterBrowRaiserR },
        { 37, (int)FacialBlendShape.OuterBrowRaiserL },
        { 38, (int)FacialBlendShape.UpperLidRaiserR },
        { 39, (int)FacialBlendShape.UpperLidRaiserL },
        { 40, (int)FacialBlendShape.UpperLipRaiserR },
        { 41, (int)FacialBlendShape.UpperLipRaiserL },
    };
    
    public static readonly Dictionary<FacialBlendShape, string> FacialBlendShapes
        = new Dictionary<FacialBlendShape, string>()
    {
        { FacialBlendShape.BrowLowererL, "Left Brow Lowerer" },
        { FacialBlendShape.BrowLowererR, "Right Brow Lowerer "},
        { FacialBlendShape.CheekRaiserL, "Left Cheek Raiser" },
        { FacialBlendShape.CheekRaiserR, "Right Cheek Raiser" },
        { FacialBlendShape.ChinRaiser, "Chin Raiser" },
        { FacialBlendShape.DimplerL, "Left Dimpler" },
        { FacialBlendShape.DimplerR, "Right Dimpler" },
        { FacialBlendShape.EyesClosedL, "Left Eye Closed" },
        { FacialBlendShape.EyesClosedR, "Right Eye Closed" },
        { FacialBlendShape.InnerBrowRaiserL, "Left Inner Brow Raiser" },
        { FacialBlendShape.InnerBrowRaiserR, "Right Inner Brow Raiser" },
        { FacialBlendShape.JawDrop, "Jaw Drop" },
        { FacialBlendShape.LidTightenerL, "Left Lid Tightener" },
        { FacialBlendShape.LidTightenerR, "Right Lid Tightener" },
        { FacialBlendShape.LipCornerDepressorL, "Left Lip Corner Depressor" },
        { FacialBlendShape.LipCornerDepressorR, "Right Lip Corner Depressor" },
        { FacialBlendShape.LipCornerPullerL, "Left Corner Puller" },
        { FacialBlendShape.LipCornerPullerR, "Right Corner Puller" },
        { FacialBlendShape.LipFunnelerLB, "Left Bottom Lip Funneler" },
        { FacialBlendShape.LipFunnelerLT, "Left Top Lip Funneler" },
        { FacialBlendShape.LipFunnelerRB, "Right Bottom Lip Funneler" },
        { FacialBlendShape.LipFunnelerRT, "Right Top Lip Funneler" },
        { FacialBlendShape.LipPressorL, "Left Lip Pressor" },
        { FacialBlendShape.LipPressorR, "Right Lip Pressor" },
        { FacialBlendShape.LipPuckerL, "Left Lip Pucker" },
        { FacialBlendShape.LipPuckerR, "Right Lip Pucker" },
        { FacialBlendShape.LipStretcherL, "Left Lip Stretcher" },
        { FacialBlendShape.LipStretcherR, "Right Lip Stretcher" },
        { FacialBlendShape.LipSuckLB, "Left Bottom Lip Suck" },
        { FacialBlendShape.LipSuckLT, "Left Top Lip Suck" },
        { FacialBlendShape.LipSuckRB, "Right Bottom Lip Suck" },
        { FacialBlendShape.LipSuckRT, "Right Top Lip Suck" },
        { FacialBlendShape.LipTightenerL, "Left Lip Tightener" },
        { FacialBlendShape.LipTightenerR, "Right Lip Tightener" },
        { FacialBlendShape.LipsToward, "Lips Toward" },
        { FacialBlendShape.LowerLipDepressorL, "Left Lower Lip Depressor" },
        { FacialBlendShape.LowerLipDepressorR, "Right Lower Lip Depressor" },
        { FacialBlendShape.NoseWrinklerL, "Left Nose Wrinkler" },
        { FacialBlendShape.NoseWrinklerR, "Right Nose Wrinkler" },
        { FacialBlendShape.OuterBrowRaiserL, "Left Outer Brow Raiser" },
        { FacialBlendShape.OuterBrowRaiserR, "Right Outer Brow Raiser" },
        { FacialBlendShape.UpperLidRaiserL, "Left Upper Lid Raiser" },
        { FacialBlendShape.UpperLidRaiserR, "Right Upper Lid Raiser" },
        { FacialBlendShape.UpperLipRaiserL, "Left Upper Lip Raiser" },
        { FacialBlendShape.UpperLipRaiserR, "Right Upper Lip Raiser" },
        { FacialBlendShape.TongueOut, "Tongue Out" }
    };
}
