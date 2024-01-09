// 
// Copyright (C) 2017, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;

#endregion

//This namespace holds Drawing tools in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.DrawingTools
{
	/// <summary>
	/// Represents an interface that exposes information regarding a Risk Reward IDrawingTool.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Always)]
	public class RiskRewardCustom : DrawingTool
    {

        private const int cursorSensitivity = 15;
        private ChartAnchor editingAnchor;
        private double entryPrice;
        private bool needsRatioUpdate = true;
        private double ratio = 1.2;

        // custom
        private double ratio2 = 2;
        private double ratio3 = 4;

        private double risk;
        private double reward;

        // custom
        private double reward2;
        private double reward3;

        private double stopPrice;
        private double targetPrice;

        // custom
        private double targetPrice2;
        private double targetPrice3;

        private double textleftPoint;
        private double textRightPoint;

        [Browsable(false)]
        private bool DrawTarget { get { return (RiskAnchor != null && !RiskAnchor.IsEditing) || (RewardAnchor != null && !RewardAnchor.IsEditing); } }

        [Display(Order = 1)]
        public ChartAnchor EntryAnchor { get; set; }
        [Display(Order = 2)]
        public ChartAnchor RiskAnchor { get; set; }
        [Browsable(false)]
        public ChartAnchor RewardAnchor { get; set; }
        [Browsable(false)]
        public ChartAnchor RewardAnchor2 { get; set; }
        [Browsable(false)]
        public ChartAnchor RewardAnchor3 { get; set; }

        [Range(0, double.MaxValue)]
        [NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolRiskRewardRatio", GroupName = "NinjaScriptGeneral", Order = 1)]
        public double Ratio
        {
            get { return ratio; }
            set
            {
                if (ratio.ApproxCompare(value) == 0)
                    return;
                ratio = value;
                needsRatioUpdate = true;
            }
        }

        [Range(0, double.MaxValue)]
        [NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Ratio2", GroupName = "NinjaScriptGeneral", Order = 1)]
        public double Ratio2
        {
            get { return ratio2; }
            set
            {
                if (ratio2.ApproxCompare(value) == 0)
                    return;
                ratio2 = value;
                needsRatioUpdate = true;
            }
        }

        [Range(0, double.MaxValue)]
        [NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Ratio3", GroupName = "NinjaScriptGeneral", Order = 1)]
        public double Ratio3
        {
            get { return ratio3; }
            set
            {
                if (ratio3.ApproxCompare(value) == 0)
                    return;
                ratio3 = value;
                needsRatioUpdate = true;
            }
        }

        [Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolAnchor", GroupName = "NinjaScriptLines", Order = 3)]
        public Stroke AnchorLineStroke { get; set; }
        [Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolRiskRewardLineStrokeEntry", GroupName = "NinjaScriptLines", Order = 6)]
        public Stroke EntryLineStroke { get; set; }
        [Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolRiskRewardLineStrokeRisk", GroupName = "NinjaScriptLines", Order = 4)]
        public Stroke StopLineStroke { get; set; }
        [Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolRiskRewardLineStrokeReward", GroupName = "NinjaScriptLines", Order = 5)]
        public Stroke TargetLineStroke { get; set; }

        public override IEnumerable<ChartAnchor> Anchors { get { return new[] { EntryAnchor, RiskAnchor, RewardAnchor, RewardAnchor2, RewardAnchor3 }; } }

        [Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolFibonacciRetracementsExtendLinesRight", GroupName = "NinjaScriptLines", Order = 2)]
        public bool IsExtendedLinesRight { get; set; }
        [Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolFibonacciRetracementsExtendLinesLeft", GroupName = "NinjaScriptLines", Order = 1)]
        public bool IsExtendedLinesLeft { get; set; }
        [Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolTextAlignment", GroupName = "NinjaScriptGeneral", Order = 2)]
        public TextLocation TextAlignment { get; set; }
        [Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolRulerYValueDisplayUnit", GroupName = "NinjaScriptGeneral", Order = 3)]
        public ValueUnit DisplayUnit { get; set; }

        public override bool SupportsAlerts { get { return true; } }

        private void DrawPriceText(ChartAnchor anchor, Point point, double price, ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale)
        {
            if (TextAlignment == TextLocation.Off)
                return;

            string priceString;
            ChartBars chartBars = GetAttachedToChartBars();

            // bars can be null while chart is initializing
            if (chartBars == null)
                return;

            priceString = GetPriceString(price, chartBars);

            Stroke color;
            textleftPoint = RiskAnchor.GetPoint(chartControl, chartPanel, chartScale).X;
            textRightPoint = EntryAnchor.GetPoint(chartControl, chartPanel, chartScale).X;

            if (anchor == RewardAnchor || anchor == RewardAnchor2 || anchor == RewardAnchor3) color = TargetLineStroke;
            else if (anchor == RiskAnchor) color = StopLineStroke;
            else if (anchor == EntryAnchor) color = EntryLineStroke;
            else color = AnchorLineStroke;

            SimpleFont wpfFont = chartControl.Properties.LabelFont ?? new SimpleFont();
            SharpDX.DirectWrite.TextFormat textFormat = wpfFont.ToDirectWriteTextFormat();
            textFormat.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
            textFormat.WordWrapping = SharpDX.DirectWrite.WordWrapping.NoWrap;
            SharpDX.DirectWrite.TextLayout textLayout = new SharpDX.DirectWrite.TextLayout(Core.Globals.DirectWriteFactory, priceString, textFormat, chartPanel.H, textFormat.FontSize);

            if (RiskAnchor.Time <= EntryAnchor.Time)
            {
                if (!IsExtendedLinesLeft && !IsExtendedLinesRight)
                    switch (TextAlignment)
                    {
                        case TextLocation.InsideLeft: point.X = textleftPoint; break;
                        case TextLocation.InsideRight: point.X = textRightPoint - textLayout.Metrics.Width; break;
                        case TextLocation.ExtremeLeft: point.X = textleftPoint; break;
                        case TextLocation.ExtremeRight: point.X = textRightPoint - textLayout.Metrics.Width; break;
                    }
                else if (IsExtendedLinesLeft && !IsExtendedLinesRight)
                    switch (TextAlignment)
                    {
                        case TextLocation.InsideLeft: point.X = textleftPoint; break;
                        case TextLocation.InsideRight: point.X = textRightPoint - textLayout.Metrics.Width; break;
                        case TextLocation.ExtremeLeft: point.X = chartPanel.X; break;
                        case TextLocation.ExtremeRight: point.X = textRightPoint - textLayout.Metrics.Width; break;
                    }
                else if (!IsExtendedLinesLeft && IsExtendedLinesRight)
                    switch (TextAlignment)
                    {
                        case TextLocation.InsideLeft: point.X = textleftPoint; break;
                        case TextLocation.InsideRight: point.X = textRightPoint - textLayout.Metrics.Width; break;
                        case TextLocation.ExtremeLeft: point.X = textleftPoint; break;
                        case TextLocation.ExtremeRight: point.X = chartPanel.W - textLayout.Metrics.Width; break;
                    }
                else if (IsExtendedLinesLeft && IsExtendedLinesRight)
                    switch (TextAlignment)
                    {
                        case TextLocation.InsideLeft: point.X = textleftPoint; break;
                        case TextLocation.InsideRight: point.X = textRightPoint - textLayout.Metrics.Width; break;
                        case TextLocation.ExtremeRight: point.X = chartPanel.W - textLayout.Metrics.Width; break;
                        case TextLocation.ExtremeLeft: point.X = chartPanel.X; break;
                    }
            }
            else if (RiskAnchor.Time >= EntryAnchor.Time)
                if (!IsExtendedLinesLeft && !IsExtendedLinesRight)
                {
                    switch (TextAlignment)
                    {
                        case TextLocation.InsideLeft: point.X = textRightPoint; break;
                        case TextLocation.InsideRight: point.X = textleftPoint - textLayout.Metrics.Width; break;
                        case TextLocation.ExtremeLeft: point.X = textRightPoint; break;
                        case TextLocation.ExtremeRight: point.X = textleftPoint - textLayout.Metrics.Width; break;
                    }
                }
                else if (IsExtendedLinesLeft && !IsExtendedLinesRight)
                    switch (TextAlignment)
                    {
                        case TextLocation.InsideLeft: point.X = textRightPoint; break;
                        case TextLocation.InsideRight: point.X = textleftPoint - textLayout.Metrics.Width; break;
                        case TextLocation.ExtremeLeft: point.X = chartPanel.X; break;
                        case TextLocation.ExtremeRight: point.X = textleftPoint - textLayout.Metrics.Width; break;
                    }
                else if (!IsExtendedLinesLeft && IsExtendedLinesRight)
                    switch (TextAlignment)
                    {
                        case TextLocation.InsideLeft: point.X = textRightPoint; break;
                        case TextLocation.InsideRight: point.X = textleftPoint - textLayout.Metrics.Width; break;
                        case TextLocation.ExtremeLeft: point.X = textRightPoint; break;
                        case TextLocation.ExtremeRight: point.X = chartPanel.W - textLayout.Metrics.Width; break;
                    }
                else if (IsExtendedLinesLeft && IsExtendedLinesRight)
                    switch (TextAlignment)
                    {
                        case TextLocation.InsideLeft: point.X = textRightPoint; break;
                        case TextLocation.InsideRight: point.X = textleftPoint - textLayout.Metrics.Width; break;
                        case TextLocation.ExtremeRight: point.X = chartPanel.W - textLayout.Metrics.Width; break;
                        case TextLocation.ExtremeLeft: point.X = chartPanel.X; break;
                    }

            RenderTarget.DrawTextLayout(new SharpDX.Vector2((float)point.X, (float)point.Y), textLayout, color.BrushDX, SharpDX.Direct2D1.DrawTextOptions.NoSnap);
        }

        public override IEnumerable<AlertConditionItem> GetAlertConditionItems()
        {
            return Anchors.Select(anchor => new AlertConditionItem
            {
                Name = anchor.DisplayName,
                ShouldOnlyDisplayName = true,
                Tag = anchor
            });
        }

        public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
        {
            switch (DrawingState)
            {
                case DrawingState.Building: return Cursors.Pen;
                case DrawingState.Moving: return IsLocked ? Cursors.No : Cursors.SizeAll;
                case DrawingState.Editing: return IsLocked ? Cursors.No : (editingAnchor == EntryAnchor ? Cursors.SizeNESW : Cursors.SizeNWSE);
                default:
                    // draw move cursor if cursor is near line path anywhere
                    Point entryAnchorPixelPoint = EntryAnchor.GetPoint(chartControl, chartPanel, chartScale);

                    // see if we are near an anchor right away. this is is cheap so no big deal to do often
                    ChartAnchor closest = GetClosestAnchor(chartControl, chartPanel, chartScale, cursorSensitivity, point);

                    if (closest != null)
                        return IsLocked ? Cursors.Arrow : (closest == EntryAnchor ? Cursors.SizeNESW : Cursors.SizeNWSE);

                    Point stopAnchorPixelPoint = RiskAnchor.GetPoint(chartControl, chartPanel, chartScale);
                    Vector anchorsVector = stopAnchorPixelPoint - entryAnchorPixelPoint;

                    // see if the mouse is along one of our lines for moving
                    if (MathHelper.IsPointAlongVector(point, entryAnchorPixelPoint, anchorsVector, cursorSensitivity))
                        return IsLocked ? Cursors.Arrow : Cursors.SizeAll;

                    if (!DrawTarget)
                        return null;

                    Point targetPoint = RewardAnchor.GetPoint(chartControl, chartPanel, chartScale);
                    Vector targetToEntryVector = targetPoint - entryAnchorPixelPoint;
                    return MathHelper.IsPointAlongVector(point, entryAnchorPixelPoint, targetToEntryVector, cursorSensitivity) ? (IsLocked ? Cursors.Arrow : Cursors.SizeAll) : null;
            }
        }

        private string GetPriceString(double price, ChartBars chartBars)
        {
            string priceString;
            double yValueEntry = AttachedTo.Instrument.MasterInstrument.RoundToTickSize(EntryAnchor.Price);
            double tickSize = AttachedTo.Instrument.MasterInstrument.TickSize;
            double pointValue = AttachedTo.Instrument.MasterInstrument.PointValue;
            switch (DisplayUnit)
            {
                case ValueUnit.Currency:
                    if (AttachedTo.Instrument.MasterInstrument.InstrumentType == InstrumentType.Forex)
                    {
                        priceString = price > yValueEntry ?
                            Core.Globals.FormatCurrency(AttachedTo.Instrument.MasterInstrument.RoundToTickSize(price - yValueEntry) / tickSize * (tickSize * pointValue * Account.All[0].ForexLotSize)) :
                            Core.Globals.FormatCurrency(AttachedTo.Instrument.MasterInstrument.RoundToTickSize(yValueEntry - price) / tickSize * (tickSize * pointValue * Account.All[0].ForexLotSize));
                    }
                    else
                    {
                        priceString = price > yValueEntry ?
                            Core.Globals.FormatCurrency(AttachedTo.Instrument.MasterInstrument.RoundToTickSize(price - yValueEntry) / tickSize * (tickSize * pointValue)) :
                            Core.Globals.FormatCurrency(AttachedTo.Instrument.MasterInstrument.RoundToTickSize(yValueEntry - price) / tickSize * (tickSize * pointValue));
                    }
                    break;
                case ValueUnit.Percent:
                    priceString = price > yValueEntry ?
                        (AttachedTo.Instrument.MasterInstrument.RoundToTickSize(price - yValueEntry) / yValueEntry).ToString("P", Core.Globals.GeneralOptions.CurrentCulture) :
                        (AttachedTo.Instrument.MasterInstrument.RoundToTickSize(yValueEntry - price) / yValueEntry).ToString("P", Core.Globals.GeneralOptions.CurrentCulture);
                    break;
                case ValueUnit.Ticks:
                    priceString = price > yValueEntry ?
                        (AttachedTo.Instrument.MasterInstrument.RoundToTickSize(price - yValueEntry) / tickSize).ToString("F0") :
                        (AttachedTo.Instrument.MasterInstrument.RoundToTickSize(yValueEntry - price) / tickSize).ToString("F0");
                    break;
                case ValueUnit.Pips:
                    priceString = price > yValueEntry ?
                        (AttachedTo.Instrument.MasterInstrument.RoundToTickSize(price - yValueEntry) / tickSize / 10).ToString("F0") :
                        (AttachedTo.Instrument.MasterInstrument.RoundToTickSize(yValueEntry - price) / tickSize / 10).ToString("F0");
                    break;
                default:
                    priceString = chartBars.Bars.Instrument.MasterInstrument.FormatPrice(price);
                    break;
            }
            return priceString;
        }

        public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
        {
            ChartPanel chartPanel = chartControl.ChartPanels[chartScale.PanelIndex];
            Point entryPoint = EntryAnchor.GetPoint(chartControl, chartPanel, chartScale);
            Point stopPoint = RiskAnchor.GetPoint(chartControl, chartPanel, chartScale);

            if (!DrawTarget)
                return new[] { entryPoint, stopPoint };

            Point targetPoint = RewardAnchor.GetPoint(chartControl, chartPanel, chartScale);
            return new[] { entryPoint, stopPoint, targetPoint };
        }

        public override bool IsAlertConditionTrue(AlertConditionItem conditionItem, Condition condition, ChartAlertValue[] values, ChartControl chartControl, ChartScale chartScale)
        {
            // dig up which anchor we are running on to determine line
            ChartAnchor chartAnchor = conditionItem.Tag as ChartAnchor;
            if (chartAnchor == null)
                return false;

            ChartPanel chartPanel = chartControl.ChartPanels[PanelIndex];
            double alertY = chartScale.GetYByValue(chartAnchor.Price);
            Point entryPoint = EntryAnchor.GetPoint(chartControl, chartPanel, chartScale);
            Point stopPoint = RiskAnchor.GetPoint(chartControl, chartPanel, chartScale);
            Point targetPoint = RewardAnchor.GetPoint(chartControl, chartPanel, chartScale);
            double anchorMinX = DrawTarget ? new[] { entryPoint.X, stopPoint.X, targetPoint.X }.Min() : new[] { entryPoint.X, stopPoint.X }.Min();
            double anchorMaxX = DrawTarget ? new[] { entryPoint.X, stopPoint.X, targetPoint.X }.Max() : new[] { entryPoint.X, stopPoint.X }.Max();
            double lineStartX = IsExtendedLinesLeft ? chartPanel.X : anchorMinX;
            double lineEndX = IsExtendedLinesRight ? chartPanel.X + chartPanel.W : anchorMaxX;

            // first thing, if our smallest x is greater than most recent bar, we have nothing to do yet.
            // do not try to check Y because lines could cross through stuff
            double firstBarX = chartControl.GetXByTime(values[0].Time);
            double firstBarY = chartScale.GetYByValue(values[0].Value);

            if (lineEndX < firstBarX) // bars passed our drawing tool
                return false;

            Point lineStartPoint = new Point(lineStartX, alertY);
            Point lineEndPoint = new Point(lineEndX, alertY);

            Point barPoint = new Point(firstBarX, firstBarY);
            // NOTE: 'left / right' is relative to if line was vertical. it can end up backwards too
            MathHelper.PointLineLocation pointLocation = MathHelper.GetPointLineLocation(lineStartPoint, lineEndPoint, barPoint);
            // for vertical things, think of a vertical line rotated 90 degrees to lay flat, where it's normal vector is 'up'
            switch (condition)
            {
                case Condition.Greater: return pointLocation == MathHelper.PointLineLocation.LeftOrAbove;
                case Condition.GreaterEqual: return pointLocation == MathHelper.PointLineLocation.LeftOrAbove || pointLocation == MathHelper.PointLineLocation.DirectlyOnLine;
                case Condition.Less: return pointLocation == MathHelper.PointLineLocation.RightOrBelow;
                case Condition.LessEqual: return pointLocation == MathHelper.PointLineLocation.RightOrBelow || pointLocation == MathHelper.PointLineLocation.DirectlyOnLine;
                case Condition.Equals: return pointLocation == MathHelper.PointLineLocation.DirectlyOnLine;
                case Condition.NotEqual: return pointLocation != MathHelper.PointLineLocation.DirectlyOnLine;
                case Condition.CrossAbove:
                case Condition.CrossBelow:
                    Predicate<ChartAlertValue> predicate = v =>
                    {
                        double barX = chartControl.GetXByTime(v.Time);
                        double barY = chartScale.GetYByValue(v.Value);
                        Point stepBarPoint = new Point(barX, barY);
                        // NOTE: 'left / right' is relative to if line was vertical. it can end up backwards too
                        MathHelper.PointLineLocation ptLocation = MathHelper.GetPointLineLocation(lineStartPoint, lineEndPoint, stepBarPoint);
                        if (condition == Condition.CrossAbove)
                            return ptLocation == MathHelper.PointLineLocation.LeftOrAbove;
                        return ptLocation == MathHelper.PointLineLocation.RightOrBelow;
                    };
                    return MathHelper.DidPredicateCross(values, predicate);
            }
            return false;
        }

        public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
        {
            return DrawingState == DrawingState.Building || Anchors.Any(a => a.Time >= firstTimeOnChart && a.Time <= lastTimeOnChart);
        }

        public override void OnCalculateMinMax()
        {
            // It is important to set MinValue and MaxValue to the min/max Y values your drawing tool uses if you want it to support auto scale
            MinValue = double.MaxValue;
            MaxValue = double.MinValue;

            if (!IsVisible)
                return;

            // return min/max values only if something has been actually drawn
            if (Anchors.Any(a => !a.IsEditing))
                foreach (ChartAnchor anchor in Anchors)
                {
                    if (anchor.DisplayName == RewardAnchor.DisplayName && !DrawTarget)
                        continue;

                    MinValue = Math.Min(anchor.Price, MinValue);
                    MaxValue = Math.Max(anchor.Price, MaxValue);
                }
        }

        public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
        {
            switch (DrawingState)
            {
                case DrawingState.Building:
                    if (EntryAnchor.IsEditing)
                    {
                        dataPoint.CopyDataValues(EntryAnchor);
                        dataPoint.CopyDataValues(RiskAnchor);
                        EntryAnchor.IsEditing = false;
                        entryPrice = AttachedTo.Instrument.MasterInstrument.RoundToTickSize(EntryAnchor.Price);
                    }
                    else if (RiskAnchor.IsEditing)
                    {
                        dataPoint.CopyDataValues(RiskAnchor);
                        RiskAnchor.IsEditing = false;
                        stopPrice = AttachedTo.Instrument.MasterInstrument.RoundToTickSize(RiskAnchor.Price);
                        SetReward();
                        // we set the anchor for the target after stop mouse down event via SetReward()
                        //however we need make sure Time is in view when builiding, but always when SetRreward is used..
                        RewardAnchor.Time = EntryAnchor.Time;
                        RewardAnchor.SlotIndex = EntryAnchor.SlotIndex;
                        RewardAnchor.IsEditing = false;
                    }
                    // if the anchors are no longer being edited, set the drawing state to normal and unselect the object
                    if (!EntryAnchor.IsEditing && !RiskAnchor.IsEditing && !RewardAnchor.IsEditing)
                    {
                        DrawingState = DrawingState.Normal;
                        IsSelected = false;
                    }
                    break;
                case DrawingState.Normal:
                    Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale);
                    //find which anchor has been clicked relative to the mouse point and make whichever anchor now editable
                    editingAnchor = GetClosestAnchor(chartControl, chartPanel, chartScale, cursorSensitivity, point);
                    if (editingAnchor != null)
                    {
                        editingAnchor.IsEditing = true;
                        DrawingState = DrawingState.Editing;
                    }
                    else if (GetCursor(chartControl, chartPanel, chartScale, point) == null)
                        IsSelected = false; // missed
                    else
                        // didnt click an anchor but on a line so start moving
                        DrawingState = DrawingState.Moving;
                    break;
            }
        }

        public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
        {
            if (IsLocked && DrawingState != DrawingState.Building || !IsVisible)
                return;

            if (DrawingState == DrawingState.Building)
            {
                if (EntryAnchor.IsEditing)
                    dataPoint.CopyDataValues(EntryAnchor);
                else if (RiskAnchor.IsEditing)
                    dataPoint.CopyDataValues(RiskAnchor);
                else if (RewardAnchor.IsEditing)
                    dataPoint.CopyDataValues(RewardAnchor);
            }
            else if (DrawingState == DrawingState.Editing && editingAnchor != null)
            {
                dataPoint.CopyDataValues(editingAnchor);
                if (editingAnchor != EntryAnchor)
                {
                    if (editingAnchor != RewardAnchor && Ratio.ApproxCompare(0) != 0)
                        SetReward();
                    else if (Ratio.ApproxCompare(0) != 0)
                        SetRisk();
                }
            }
            else if (DrawingState == DrawingState.Moving)
            {
                foreach (ChartAnchor anchor in Anchors)
                    anchor.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
            }

            entryPrice = AttachedTo.Instrument.MasterInstrument.RoundToTickSize(EntryAnchor.Price);
            stopPrice = AttachedTo.Instrument.MasterInstrument.RoundToTickSize(RiskAnchor.Price);
            targetPrice = AttachedTo.Instrument.MasterInstrument.RoundToTickSize(RewardAnchor.Price);
        }

        public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
        {
            //don't set anchors until we're done drawing
            if (DrawingState == DrawingState.Building)
                return;

            //set the drawing state back to normal when mouse is relased
            if (DrawingState == DrawingState.Editing || DrawingState == DrawingState.Moving)
                DrawingState = DrawingState.Normal;
            if (editingAnchor != null)
            {
                if (editingAnchor == EntryAnchor)
                {
                    SetReward();
                    if (Ratio.ApproxCompare(0) != 0)
                        SetRisk();
                }
                editingAnchor.IsEditing = false;
            }
            editingAnchor = null;
        }

        public override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            if (!IsVisible)
                return;
            if (Anchors.All(a => a.IsEditing))
                return;

            // this will be true right away to fix a restoral issue, so check if we really want to set reward
            if (needsRatioUpdate && DrawTarget)
                SetReward();

            ChartPanel chartPanel = chartControl.ChartPanels[PanelIndex];
            Point entryPoint = EntryAnchor.GetPoint(chartControl, chartPanel, chartScale);
            Point stopPoint = RiskAnchor.GetPoint(chartControl, chartPanel, chartScale);
            Point targetPoint = RewardAnchor.GetPoint(chartControl, chartPanel, chartScale);

            // custom
            Point targetPoint2 = RewardAnchor2.GetPoint(chartControl, chartPanel, chartScale);
            Point targetPoint3 = RewardAnchor3.GetPoint(chartControl, chartPanel, chartScale);

            AnchorLineStroke.RenderTarget = RenderTarget;
            EntryLineStroke.RenderTarget = RenderTarget;
            StopLineStroke.RenderTarget = RenderTarget;

            // first of all, turn on anti-aliasing to smooth out our line
            RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.PerPrimitive;
            RenderTarget.DrawLine(entryPoint.ToVector2(), stopPoint.ToVector2(), AnchorLineStroke.BrushDX, AnchorLineStroke.Width, AnchorLineStroke.StrokeStyle);

            double anchorMinX = DrawTarget ? new[] { entryPoint.X, stopPoint.X, targetPoint.X }.Min() : new[] { entryPoint.X, stopPoint.X }.Min();
            double anchorMaxX = DrawTarget ? new[] { entryPoint.X, stopPoint.X, targetPoint.X }.Max() : new[] { entryPoint.X, stopPoint.X }.Max();
            double lineStartX = IsExtendedLinesLeft ? chartPanel.X : anchorMinX;
            double lineEndX = IsExtendedLinesRight ? chartPanel.X + chartPanel.W : anchorMaxX;

            SharpDX.Vector2 entryStartVector = new SharpDX.Vector2((float)lineStartX, (float)entryPoint.Y);
            SharpDX.Vector2 entryEndVector = new SharpDX.Vector2((float)lineEndX, (float)entryPoint.Y);
            SharpDX.Vector2 stopStartVector = new SharpDX.Vector2((float)lineStartX, (float)stopPoint.Y);
            SharpDX.Vector2 stopEndVector = new SharpDX.Vector2((float)lineEndX, (float)stopPoint.Y);

            // don't try and draw the target stuff until we have calculated the target
            SharpDX.Direct2D1.Brush tmpBrush = IsInHitTest ? chartControl.SelectionBrush : AnchorLineStroke.BrushDX;
            if (DrawTarget)
            {
                AnchorLineStroke.RenderTarget = RenderTarget;
                RenderTarget.DrawLine(entryPoint.ToVector2(), targetPoint.ToVector2(), tmpBrush, AnchorLineStroke.Width, AnchorLineStroke.StrokeStyle);               

                TargetLineStroke.RenderTarget = RenderTarget;
                SharpDX.Vector2 targetStartVector = new SharpDX.Vector2((float)lineStartX, (float)targetPoint.Y);
                SharpDX.Vector2 targetEndVector = new SharpDX.Vector2((float)lineEndX, (float)targetPoint.Y);

                tmpBrush = IsInHitTest ? chartControl.SelectionBrush : TargetLineStroke.BrushDX;
                RenderTarget.DrawLine(targetStartVector, targetEndVector, tmpBrush, TargetLineStroke.Width, TargetLineStroke.StrokeStyle);
                DrawPriceText(RewardAnchor, targetPoint, targetPrice, chartControl, chartPanel, chartScale);

                // custom
                SharpDX.Vector2 targetStartVector2 = new SharpDX.Vector2((float)lineStartX, (float)targetPoint2.Y);
                SharpDX.Vector2 targetEndVector2 = new SharpDX.Vector2((float)lineEndX, (float)targetPoint2.Y);
                SharpDX.Vector2 targetStartVector3 = new SharpDX.Vector2((float)lineStartX, (float)targetPoint3.Y);
                SharpDX.Vector2 targetEndVector3 = new SharpDX.Vector2((float)lineEndX, (float)targetPoint3.Y);

                RenderTarget.DrawLine(targetStartVector2, targetEndVector2, tmpBrush, TargetLineStroke.Width, TargetLineStroke.StrokeStyle);
                DrawPriceText(RewardAnchor2, targetPoint2, targetPrice2, chartControl, chartPanel, chartScale);
                RenderTarget.DrawLine(targetStartVector3, targetEndVector3, tmpBrush, TargetLineStroke.Width, TargetLineStroke.StrokeStyle);
                DrawPriceText(RewardAnchor3, targetPoint3, targetPrice3, chartControl, chartPanel, chartScale);

            }

            tmpBrush = IsInHitTest ? chartControl.SelectionBrush : EntryLineStroke.BrushDX;
            RenderTarget.DrawLine(entryStartVector, entryEndVector, tmpBrush, EntryLineStroke.Width, EntryLineStroke.StrokeStyle);
            DrawPriceText(EntryAnchor, entryPoint, entryPrice, chartControl, chartPanel, chartScale);

            tmpBrush = IsInHitTest ? chartControl.SelectionBrush : StopLineStroke.BrushDX;
            RenderTarget.DrawLine(stopStartVector, stopEndVector, tmpBrush, StopLineStroke.Width, StopLineStroke.StrokeStyle);
            DrawPriceText(RiskAnchor, stopPoint, stopPrice, chartControl, chartPanel, chartScale);
        }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = Custom.Resource.NinjaScriptDrawingToolRiskRewardDescription;
                Name = Custom.Resource.NinjaScriptDrawingToolRiskRewardName;
                Ratio = 2;
                AnchorLineStroke = new Stroke(Brushes.DarkGray, DashStyleHelper.Solid, 1f, 50);
                EntryLineStroke = new Stroke(Brushes.Goldenrod, DashStyleHelper.Solid, 2f);
                StopLineStroke = new Stroke(Brushes.Crimson, DashStyleHelper.Solid, 2f);
                TargetLineStroke = new Stroke(Brushes.SeaGreen, DashStyleHelper.Solid, 2f);
                EntryAnchor = new ChartAnchor { IsEditing = true, DrawingTool = this };
                RiskAnchor = new ChartAnchor { IsEditing = true, DrawingTool = this };
                RewardAnchor = new ChartAnchor { IsEditing = true, DrawingTool = this };

                // custom
                RewardAnchor2 = new ChartAnchor { IsEditing = true, DrawingTool = this };
                RewardAnchor3 = new ChartAnchor { IsEditing = true, DrawingTool = this };

                EntryAnchor.DisplayName = Custom.Resource.NinjaScriptDrawingToolRiskRewardAnchorEntry;
                RiskAnchor.DisplayName = Custom.Resource.NinjaScriptDrawingToolRiskRewardAnchorRisk;
                RewardAnchor.DisplayName = Custom.Resource.NinjaScriptDrawingToolRiskRewardAnchorReward;
            }
            else if (State == State.Terminated)
                Dispose();
        }
		
		private void SetReward()
		{
			if (Anchors == null || AttachedTo == null)
				return;
			
			entryPrice				= AttachedTo.Instrument.MasterInstrument.RoundToTickSize(EntryAnchor.Price);
			stopPrice 				= AttachedTo.Instrument.MasterInstrument.RoundToTickSize(RiskAnchor.Price);
			risk 					= entryPrice - stopPrice;
			reward					= risk * Ratio;

            // custom
            reward2 = risk * Ratio2;
            reward3 = risk * Ratio3;

			targetPrice 			= AttachedTo.Instrument.MasterInstrument.RoundToTickSize(entryPrice + reward);

            // custom
			targetPrice2 			= AttachedTo.Instrument.MasterInstrument.RoundToTickSize(entryPrice + reward2);
			targetPrice3 			= AttachedTo.Instrument.MasterInstrument.RoundToTickSize(entryPrice + reward3);
			
			RewardAnchor.Price		= targetPrice;

            // custom
            RewardAnchor2.Price = targetPrice2;
            RewardAnchor3.Price = targetPrice3;

			RewardAnchor.IsEditing	= false;
		
			needsRatioUpdate		= false;
		}

        private void SetRisk()
        {
            if (Anchors == null || AttachedTo == null)
                return;

            entryPrice = AttachedTo.Instrument.MasterInstrument.RoundToTickSize(EntryAnchor.Price);
            targetPrice = AttachedTo.Instrument.MasterInstrument.RoundToTickSize(RewardAnchor.Price);

            reward = targetPrice - entryPrice;
            risk = reward / Ratio;
            stopPrice = AttachedTo.Instrument.MasterInstrument.RoundToTickSize(entryPrice - risk);

            RiskAnchor.Price = stopPrice;
            RiskAnchor.IsEditing = false;

            needsRatioUpdate = false;
        }

    }
}