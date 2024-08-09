// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Controls.Primitives;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;

namespace Files.App.Controls
{
    // TemplateParts
    [TemplatePart( Name = ContainerPartName			, Type = typeof( Grid ) )]
    [TemplatePart( Name = ValueRingShapePartName	, Type = typeof( RingShape ) )]
    [TemplatePart( Name = TrackRingShapePartName	, Type = typeof( RingShape ) )]

    // VisualStates
    [TemplateVisualState( Name = SafeStateName		, GroupName = ControlStateGroupName )]
    [TemplateVisualState( Name = CautionStateName	, GroupName = ControlStateGroupName )]
    [TemplateVisualState( Name = CriticalStateName	, GroupName = ControlStateGroupName )]
    [TemplateVisualState( Name = DisabledStateName	, GroupName = ControlStateGroupName )]




    public partial class StorageRing : RangeBase
    {
        internal const string ContainerPartName         = "PART_Container";
        internal const string ValueRingShapePartName    = "PART_ValueRingShape";
        internal const string TrackRingShapePartName    = "PART_TrackRingShape";

        internal const string ControlStateGroupName     = "ControlStates";

        internal const string SafeStateName             = "Safe";
        internal const string CautionStateName          = "Caution";
        internal const string CriticalStateName         = "Critical";
        internal const string DisabledStateName         = "Disabled";

        internal const double Degrees2Radians			= Math.PI / 180;
    }
}
