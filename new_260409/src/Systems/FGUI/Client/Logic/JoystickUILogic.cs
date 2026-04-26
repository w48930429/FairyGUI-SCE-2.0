#if CLIENT
using System.Drawing;
using FairyGUI;

namespace GameEntry;

internal sealed partial class JoystickUILogic : FguiUILogicBase
{
    private GTextField? outputText;
    private GObject? touchArea;
    private GObject? center;
    private GButton? joystick;
    private GObject? thumb;
    private readonly object returnTweenToken = new();
    private float initX;
    private float initY;
    private float startX;
    private float startY;
    private float lastX;
    private float lastY;
    private int moveLogCount;
    private bool touching;
    private int touchId = -1;

    public override string PackageName => "Joystick";

    public override bool TryBind(GComponent view, out string message)
    {
        Root = view;
        BindCoreControls(view);
        outputText = FindTextFieldByName(view, "n9") ?? FindFirstTextField(view);

        if (touchArea == null || center == null || joystick == null)
        {
            message = "bind failed: joystick core fields missing (joystick_touch/joystick_center/joystick)";
            Game.Logger.LogWarning("[FGUI][Joystick] bind failed: core fields missing.");
            return false;
        }

        initX = center.X + center.Width / 2f;
        initY = center.Y + center.Height / 2f;
        touching = false;
        touchId = -1;
        moveLogCount = 0;
        joystick.ChangeStateOnClick = false;

        touchArea.Touchable = true;
        center.Visible = true;
        joystick.Visible = true;

        touchArea.OnTouchBegin.Add(OnTouchBegin);
        touchArea.OnTouchMove.Add(OnTouchMove);
        touchArea.OnTouchEnd.Add(OnTouchEnd);
        message = outputText == null ? "joystick logic bound (angle text optional missing)" : "joystick logic bound";
        Game.Logger.LogWarning(
            "[FGUI][Joystick] bind ok typed={Typed} touch={Touch} center={Center} joystick={Joystick} thumb={Thumb} output={Output}",
            view is global::Joystick.Main,
            touchArea.Name,
            center.Name,
            joystick.Name,
            thumb?.Name ?? "<none>",
            outputText?.Name ?? "<none>");
        return true;
    }

    public override void Cleanup()
    {
        if (touchArea != null)
        {
            touchArea.OnTouchBegin.Remove(OnTouchBegin);
            touchArea.OnTouchMove.Remove(OnTouchMove);
            touchArea.OnTouchEnd.Remove(OnTouchEnd);
        }

        GTween.Kill(returnTweenToken);
        outputText = null;
        touchArea = null;
        center = null;
        joystick = null;
        thumb = null;
        touching = false;
        touchId = -1;
        moveLogCount = 0;
        Root = null;
    }

    private void OnTouchBegin(EventContext ctx)
    {
        if (touching || center == null || joystick == null || !TryResolveTouchPoint(ctx, out var rawX, out var rawY, out var eventTouchId))
        {
            return;
        }

        ResolveTouchPoint(rawX, rawY, out var bx, out var by, preferContinuity: false);
        ClampToTouchArea(ref bx, ref by);
        touching = true;
        touchId = eventTouchId;
        moveLogCount = 0;
        GTween.Kill(returnTweenToken);

        lastX = bx;
        lastY = by;
        startX = initX;
        startY = initY;

        joystick.Selected = true;
        center.Visible = true;
        center.SetXY(initX - center.Width / 2f, initY - center.Height / 2f);
        joystick.SetXY(initX - joystick.Width / 2f, initY - joystick.Height / 2f);
        UpdateDirectionTextAndThumb(bx - startX, by - startY);
        Game.Logger.LogWarning(
            "[FGUI][Joystick] touch-begin raw={RawX:F1},{RawY:F1} local={X:F1},{Y:F1} touchId={TouchId} fixedCenter={StartX:F1},{StartY:F1} joystick={JoyX:F1},{JoyY:F1}",
            rawX,
            rawY,
            bx,
            by,
            touchId,
            startX,
            startY,
            joystick.X,
            joystick.Y);
    }

    private void OnTouchMove(EventContext ctx)
    {
        if (!touching || joystick == null || center == null || !TryResolveTouchPoint(ctx, out var rawX, out var rawY, out var eventTouchId))
        {
            return;
        }

        if (touchId >= 0 && eventTouchId >= 0 && eventTouchId != touchId)
        {
            return;
        }

        ResolveTouchPoint(rawX, rawY, out var bx, out var by, preferContinuity: true);
        ClampToTouchArea(ref bx, ref by);
        lastX = bx;
        lastY = by;

        // Use absolute vector from fixed center to current touch.
        // Delta-based movement can lock one direction when begin point is clamped at edge.
        var offsetX = bx - startX;
        var offsetY = by - startY;
        var radius = 150f;
        var length = MathF.Sqrt(offsetX * offsetX + offsetY * offsetY);
        if (length > radius && length > 0.001f)
        {
            var scale = radius / length;
            offsetX *= scale;
            offsetY *= scale;
        }

        var buttonX = startX + offsetX;
        var buttonY = startY + offsetY;
        joystick.SetXY(buttonX - joystick.Width / 2f, buttonY - joystick.Height / 2f);
        UpdateDirectionTextAndThumb(offsetX, offsetY);
        if (moveLogCount < 8)
        {
            moveLogCount++;
            Game.Logger.LogWarning(
                "[FGUI][Joystick] touch-move raw={RawX:F1},{RawY:F1} local={X:F1},{Y:F1} offset={OffsetX:F1},{OffsetY:F1} joy={JoyX:F1},{JoyY:F1} sample={Sample}",
                rawX,
                rawY,
                bx,
                by,
                offsetX,
                offsetY,
                joystick.X,
                joystick.Y,
                moveLogCount);
        }
    }

    private void OnTouchEnd(EventContext ctx)
    {
        if (!touching || joystick == null || center == null)
        {
            return;
        }

        if (TryResolveTouchPoint(ctx, out _, out _, out var eventTouchId)
            && touchId >= 0
            && eventTouchId >= 0
            && eventTouchId != touchId)
        {
            return;
        }

        touching = false;
        touchId = -1;
        if (outputText != null)
        {
            outputText.Text = string.Empty;
        }

        var start = new PointF(joystick.X, joystick.Y);
        var end = new PointF(initX - joystick.Width / 2f, initY - joystick.Height / 2f);
        GTween.Kill(returnTweenToken);
        GTween.To(start, end, 0.3f)
            .SetTarget(returnTweenToken)
            .SetEase(EaseType.QuadOut)
            .OnUpdate(t => joystick.SetXY(t.Value.X, t.Value.Y))
            .OnComplete(_ =>
            {
                joystick.Selected = false;
                center.Visible = true;
                center.SetXY(initX - center.Width / 2f, initY - center.Height / 2f);
                if (thumb != null)
                {
                    thumb.Rotation = 0f;
                }
            });
    }

    private void UpdateDirectionTextAndThumb(float dx, float dy)
    {
        var degree = MathF.Atan2(dy, dx) * 180f / MathF.PI;
        if (outputText != null)
        {
            outputText.Text = $"{MathF.Round(degree)}";
        }

        if (thumb != null)
        {
            thumb.Rotation = degree + 90f;
        }
    }

    private void BindCoreControls(GComponent view)
    {
        if (view is global::Joystick.Main typedMain)
        {
            touchArea = typedMain.joystick_touch;
            center = typedMain.joystick_center;
            joystick = typedMain.joystick;
            thumb = typedMain.joystick?.thumb;
            return;
        }

        touchArea = FindChildRecursive(view, "joystick_touch");
        center = FindChildRecursive(view, "joystick_center");
        joystick = FindButtonByName(view, "joystick");
        thumb = FindChildRecursive(joystick ?? view, "thumb");
    }

    private static GTextField? FindFirstTextField(GObject root)
    {
        foreach (var node in EnumerateChildrenRecursive(root))
        {
            if (node is GTextField tf)
            {
                return tf;
            }
        }

        return null;
    }

    private static bool TryResolveTouchPoint(EventContext ctx, out float x, out float y, out int resolvedTouchId)
    {
        x = 0f;
        y = 0f;
        resolvedTouchId = -1;
        if (ctx.Data is TouchEventData touch)
        {
            x = touch.Position.X;
            y = touch.Position.Y;
            resolvedTouchId = touch.TouchId;
            return true;
        }

        if (ctx.Data is PointF point)
        {
            x = point.X;
            y = point.Y;
            return true;
        }

        return false;
    }

    private void ResolveTouchPoint(float rawX, float rawY, out float x, out float y, bool preferContinuity)
    {
        _ = preferContinuity;
        var scale = UIRuntime.ContentScaleFactor;
        if (scale <= 0.0001f)
        {
            scale = 1f;
        }

        // Native pointer position is in scaled screen space.
        // Convert once to logical space, then map to Root local space.
        var logicalX = rawX / scale;
        var logicalY = rawY / scale;
        x = logicalX;
        y = logicalY;

        if (Root != null)
        {
            GetAbsolutePosition(Root, out var rootAbsX, out var rootAbsY);
            x = logicalX - rootAbsX;
            y = logicalY - rootAbsY;
        }

        if (Root != null && Root.Width > 1f && Root.Height > 1f)
        {
            x = Math.Clamp(x, 0f, Root.Width);
            y = Math.Clamp(y, 0f, Root.Height);
        }
    }

    private void ClampToTouchArea(ref float x, ref float y)
    {
        if (touchArea == null)
        {
            return;
        }

        var minX = touchArea.X;
        var minY = touchArea.Y;
        var maxX = touchArea.X + MathF.Max(0f, touchArea.Width);
        var maxY = touchArea.Y + MathF.Max(0f, touchArea.Height);
        x = Math.Clamp(x, minX, maxX);
        y = Math.Clamp(y, minY, maxY);
    }

    private static void GetAbsolutePosition(GObject obj, out float x, out float y)
    {
        x = obj.X;
        y = obj.Y;

        var parent = obj.Parent;
        while (parent != null)
        {
            x += parent.X;
            y += parent.Y;
            parent = parent.Parent;
        }
    }

}

#endif



