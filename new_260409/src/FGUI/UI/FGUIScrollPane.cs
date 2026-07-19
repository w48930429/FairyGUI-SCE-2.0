#if CLIENT
using System.Drawing;
using FairyGUI;
using FairyGUI.Render;
using FairyGUI.Utils;

namespace FairyGUI;

public class ScrollPane
{
    public GComponent? Owner { get; internal set; }

    private ScrollType _scrollType = ScrollType.Vertical;
    private float _scrollSpeed = 1;
    private bool _mouseWheelEnabled = true;
    private float _decelerationRate = 0.967f;
    private bool _touchEffect = true;
    private bool _bouncebackEffect = true;
    private bool _inertiaDisabled;
    private float _scrollBarMargin;
    private ScrollBarDisplayType _scrollBarDisplayType = ScrollBarDisplayType.Default;

    private float _viewWidth, _viewHeight;
    private float _contentWidth, _contentHeight;
    private float _xPos, _yPos;
    private float _velocityX, _velocityY;
    private float _clipSoftnessX, _clipSoftnessY;
    private bool _scrolling;
    private bool _dragging;
    private PointF _lastTouchPos;
    private float _lastTouchTime;
    private bool _isHolding;

    // Scrollbars
    private GObject? _hScrollBar;
    private GObject? _vScrollBar;
    private bool _hScrollBarVisible;
    private bool _vScrollBarVisible;

    // Tweening
    private GTweener? _tweener;
    private bool _tweening;

    // Pagination
    private bool _pageMode;
    private Controller? _pageController;
    private float _pageWidth, _pageHeight;
    private bool _snapToItem;

    // Pull to refresh
    private GComponent? _header;
    private GComponent? _footer;
    private int _headerLockedSize;
    private int _footerLockedSize;

    // Constants
    private const float SCROLL_THRESHOLD = 5f;

    private const float OVERSCROLL_DAMP_RANGE_RATIO = 0.5f;

    private const float INERTIA_DISTANCE_FACTOR = 0.3f;
    private const float INERTIA_VELOCITY_THRESHOLD = 100f;
    private const float INERTIA_DURATION_DIVISOR = 600f;
    private const float SCROLL_ANIM_MIN_DURATION = 0.2f;
    private const float SCROLL_ANIM_MAX_DURATION = 0.8f;

    public ScrollType ScrollType => _scrollType;
    public float PosX { get => _xPos; set => SetPos(value, _yPos, false); }
    public float PosY { get => _yPos; set => SetPos(_xPos, value, false); }
    public float PercentX 
    { 
        get => _contentWidth > _viewWidth ? _xPos / (_contentWidth - _viewWidth) : 0; 
        set => SetPos(value * Math.Max(0, _contentWidth - _viewWidth), _yPos, false); 
    }
    public float PercentY 
    { 
        get => _contentHeight > _viewHeight ? _yPos / (_contentHeight - _viewHeight) : 0; 
        set => SetPos(_xPos, value * Math.Max(0, _contentHeight - _viewHeight), false); 
    }
    public float ContentWidth => _contentWidth;
    public float ContentHeight => _contentHeight;
    public float ViewWidth => _viewWidth;
    public float ViewHeight => _viewHeight;
    public bool IsScrolling => _scrolling;
    public bool IsDragging => _dragging;
    public float ClipSoftnessX => _clipSoftnessX;
    public float ClipSoftnessY => _clipSoftnessY;
    public void SetClipSoftness(float x, float y)
    {
        x = float.IsFinite(x) ? Math.Max(0f, x) : 0f;
        y = float.IsFinite(y) ? Math.Max(0f, y) : 0f;
        if (_clipSoftnessX == x && _clipSoftnessY == y)
            return;

        _clipSoftnessX = x;
        _clipSoftnessY = y;
        Owner?.UpdateClipSoftness();
    }
    public bool IsTweening => _tweening;
    public bool TouchEffect { get => _touchEffect; set => _touchEffect = value; }
    public bool BouncebackEffect { get => _bouncebackEffect; set => _bouncebackEffect = value; }
    public bool MouseWheelEnabled { get => _mouseWheelEnabled; set => _mouseWheelEnabled = value; }
    public float DecelerationRate { get => _decelerationRate; set => _decelerationRate = value; }
    public float ScrollSpeed { get => _scrollSpeed; set => _scrollSpeed = value; }
    
    public float ScrollingPosX => _xPos;
    public float ScrollingPosY => _yPos;

    public ScrollBarDisplayType ScrollBarDisplay => _scrollBarDisplayType;

    public bool PageMode
    {
        get => _pageMode;
        set
        {
            if (_pageMode == value)
                return;

            _pageMode = value;
            if (_pageMode)
            {
                _pageWidth = _viewWidth;
                _pageHeight = _viewHeight;
            }
        }
    }
    public Controller? PageController { get => _pageController; set => _pageController = value; }
    public bool SnapToItem { get => _snapToItem; set => _snapToItem = value; }

    public int CurrentPageX
    {
        get
        {
            if (!_pageMode || _pageWidth <= 0)
                return 0;

            int page = (int)Math.Floor(_xPos / _pageWidth);
            if (_xPos - page * _pageWidth > _pageWidth * 0.5f)
                page++;

            return page;
        }
        set
        {
            if (!_pageMode)
                return;

            Owner?.EnsureBoundsCorrect();

            if (_contentWidth > _viewWidth)
                SetPos(value * _pageWidth, _yPos, false);
        }
    }

    public int CurrentPageY
    {
        get
        {
            if (!_pageMode || _pageHeight <= 0)
                return 0;

            int page = (int)Math.Floor(_yPos / _pageHeight);
            if (_yPos - page * _pageHeight > _pageHeight * 0.5f)
                page++;

            return page;
        }
        set
        {
            if (!_pageMode)
                return;

            Owner?.EnsureBoundsCorrect();

            if (_contentHeight > _viewHeight)
                SetPos(_xPos, value * _pageHeight, false);
        }
    }

    public GComponent? Header { get => _header; set => _header = value; }
    public GComponent? Footer { get => _footer; set => _footer = value; }

    public EventListener OnPullDownRelease => GetOrCreateListener("onPullDownRelease");
    public EventListener OnPullUpRelease => GetOrCreateListener("onPullUpRelease");

    private readonly Dictionary<string, EventListener> _listeners = new();

    private EventListener GetOrCreateListener(string type)
    {
        if (!_listeners.TryGetValue(type, out var listener))
        {
            listener = new EventListener();
            _listeners[type] = listener;
        }
        return listener;
    }

    public void SetPos(float xv, float yv, bool animate = false)
    {
        if (_tweening)
        {
            KillTween();
        }

        xv = ClampX(xv);
        yv = ClampY(yv);

        if (animate && (_xPos != xv || _yPos != yv))
        {
            float startX = _xPos;
            float startY = _yPos;
            float endX = xv;
            float endY = yv;
            
            float dx = Math.Abs(endX - startX);
            float dy = Math.Abs(endY - startY);
            float duration = Math.Max(dx, dy) / INERTIA_DURATION_DIVISOR;
            duration = Math.Clamp(duration, SCROLL_ANIM_MIN_DURATION, SCROLL_ANIM_MAX_DURATION);
            
            _tweening = true;
            _tweener = GTween.To(new PointF(startX, startY), new PointF(endX, endY), duration)
                .SetEase(EaseType.QuadOut)
                .OnUpdate(t =>
                {
                    _xPos = t.Value.X;
                    _yPos = t.Value.Y;
                    UpdateScrollPosition();
                })
                .OnComplete(t =>
                {
                    _tweening = false;
                    _tweener = null;
                    OnScrollEnd();
                });
        }
        else if (_xPos != xv || _yPos != yv)
        {
            _xPos = xv;
            _yPos = yv;
            UpdateScrollPosition();
        }
    }

    public void ScrollTop(bool animate = false) => SetPos(_xPos, 0, animate);
    public void ScrollBottom(bool animate = false) => SetPos(_xPos, Math.Max(0, _contentHeight - _viewHeight), animate);
    public void ScrollLeft(bool animate = false) => SetPos(0, _yPos, animate);
    public void ScrollRight(bool animate = false) => SetPos(Math.Max(0, _contentWidth - _viewWidth), _yPos, animate);

    private const float DEFAULT_SCROLL_STEP = 40f;

    public void ScrollUp(float ratio = 1, bool animate = false)
    {
        if (_pageMode)
            SetPos(_xPos, _yPos - _pageHeight * ratio, animate);
        else
            SetPos(_xPos, _yPos - DEFAULT_SCROLL_STEP * ratio, animate);
    }

    public void ScrollDown(float ratio = 1, bool animate = false)
    {
        if (_pageMode)
            SetPos(_xPos, _yPos + _pageHeight * ratio, animate);
        else
            SetPos(_xPos, _yPos + DEFAULT_SCROLL_STEP * ratio, animate);
    }

    public void ScrollLeftStep(float ratio = 1, bool animate = false)
    {
        if (_pageMode)
            SetPos(_xPos - _pageWidth * ratio, _yPos, animate);
        else
            SetPos(_xPos - DEFAULT_SCROLL_STEP * ratio, _yPos, animate);
    }

    public void ScrollRightStep(float ratio = 1, bool animate = false)
    {
        if (_pageMode)
            SetPos(_xPos + _pageWidth * ratio, _yPos, animate);
        else
            SetPos(_xPos + DEFAULT_SCROLL_STEP * ratio, _yPos, animate);
    }

    public void SetCurrentPageX(int value, bool animate = false)
    {
        if (!_pageMode)
            return;

        Owner?.EnsureBoundsCorrect();

        if (_contentWidth > _viewWidth)
            SetPos(value * _pageWidth, _yPos, animate);
    }

    public void SetCurrentPageY(int value, bool animate = false)
    {
        if (!_pageMode)
            return;

        Owner?.EnsureBoundsCorrect();

        if (_contentHeight > _viewHeight)
            SetPos(_xPos, value * _pageHeight, animate);
    }

    public void LockHeader(int size)
    {
        if (_headerLockedSize == size)
            return;

        _headerLockedSize = size;

        if (_header != null)
        {
            _header.Visible = size > 0;
            if (size > 0)
                _header.Height = size;
        }
    }

    public void LockFooter(int size)
    {
        if (_footerLockedSize == size)
            return;

        _footerLockedSize = size;

        if (_footer != null)
        {
            _footer.Visible = size > 0;
            if (size > 0)
                _footer.Height = size;
        }
    }

    public void ScrollToView(GObject obj) => ScrollToView(obj, false, false);
    public void ScrollToView(GObject obj, bool animate) => ScrollToView(obj, animate, false);

    public void ScrollToView(GObject obj, bool animate, bool setFirst)
    {
        if (obj == null || Owner == null)
            return;

        Owner.EnsureBoundsCorrect();

        RectangleF rect = new RectangleF(obj.X, obj.Y, obj.Width, obj.Height);

        if (obj.Parent != null && obj.Parent != Owner)
        {
            var parent = obj.Parent;
            while (parent != null && parent != Owner)
            {
                rect.X += parent.X;
                rect.Y += parent.Y;
                parent = parent.Parent;
            }
        }

        ScrollToView(rect, animate, setFirst);
    }

    public void ScrollToView(RectangleF rect, bool animate = false, bool setFirst = false)
    {
        float targetX = _xPos;
        float targetY = _yPos;

        if (_scrollType == ScrollType.Vertical || _scrollType == ScrollType.Both)
        {
            if (setFirst || rect.Y < _yPos || rect.Height >= _viewHeight)
                targetY = rect.Y;
            else if (rect.Bottom > _yPos + _viewHeight)
                targetY = rect.Bottom - _viewHeight;
        }

        if (_scrollType == ScrollType.Horizontal || _scrollType == ScrollType.Both)
        {
            if (setFirst || rect.X < _xPos || rect.Width >= _viewWidth)
                targetX = rect.X;
            else if (rect.Right > _xPos + _viewWidth)
                targetX = rect.Right - _viewWidth;
        }

        SetPos(targetX, targetY, animate);
    }

    public bool IsChildInView(GObject obj)
    {
        if (obj == null || Owner == null)
            return false;

        if (_scrollType == ScrollType.Vertical || _scrollType == ScrollType.Both)
        {
            if (_contentHeight > _viewHeight)
            {
                float objTop = obj.Y;
                float objBottom = obj.Y + obj.Height;
                float viewTop = _yPos;
                float viewBottom = _yPos + _viewHeight;

                if (objBottom <= viewTop || objTop >= viewBottom)
                    return false;
            }
        }

        if (_scrollType == ScrollType.Horizontal || _scrollType == ScrollType.Both)
        {
            if (_contentWidth > _viewWidth)
            {
                float objLeft = obj.X;
                float objRight = obj.X + obj.Width;
                float viewLeft = _xPos;
                float viewRight = _xPos + _viewWidth;

                if (objRight <= viewLeft || objLeft >= viewRight)
                    return false;
            }
        }

        return true;
    }

    public void CancelDragging()
    {
        _dragging = false;
        _isHolding = false;
    }

    private static float ResistedMove(float pos, float delta, float max, float dim)
    {
        if (delta == 0f || dim <= 0f)
            return pos + delta;

        float range = dim * OVERSCROLL_DAMP_RANGE_RATIO;
        if (range <= 0f)
            return pos;

        if (delta > 0f)
        {
            if (pos < 0f)
            {
                float backToEdge = -pos;
                if (delta <= backToEdge)
                    return pos + delta;
                delta -= backToEdge;
                pos = 0f;
            }

            if (pos < max)
            {
                float toFarEdge = max - pos;
                if (delta <= toFarEdge)
                    return pos + delta;
                delta -= toFarEdge;
                pos = max;
            }

            return max + ApplyOutwardResistance(pos - max, delta, range);
        }

        float magnitude = -delta;
        if (pos > max)
        {
            float backToEdge = pos - max;
            if (magnitude <= backToEdge)
                return pos + delta;
            magnitude -= backToEdge;
            pos = max;
        }

        if (pos > 0f)
        {
            float toNearEdge = pos;
            if (magnitude <= toNearEdge)
                return pos + delta;
            magnitude -= toNearEdge;
            pos = 0f;
        }

        return -ApplyOutwardResistance(-pos, magnitude, range);
    }

    private static float ApplyOutwardResistance(float overshoot, float outwardDelta, float range)
    {
        if (overshoot >= range)
            return overshoot;

        return range - (range - Math.Max(0f, overshoot)) * MathF.Exp(-outwardDelta / range);
    }

    private float ClampX(float x)
    {
        float max = _contentWidth - _viewWidth;
        if (max <= 0) return 0;
        return Math.Clamp(x, 0, max);
    }

    private float ClampY(float y)
    {
        float max = _contentHeight - _viewHeight;
        if (max <= 0) max = 0;

        float min = 0;
        if (_headerLockedSize > 0) min = -_headerLockedSize;
        if (_footerLockedSize > 0) max += _footerLockedSize;

        return Math.Clamp(y, min, max);
    }

    private void UpdateScrollPosition()
    {
        Owner?.DispatchEvent("onScroll", null);
        UpdateScrollBars();

        if ((_clipSoftnessX > 0 || _clipSoftnessY > 0) && Owner != null)
        {
            Owner.UpdateClipSoftness();
        }

        if (_pageMode)
            UpdatePageController();

        if (Owner != null)
        {
            SCERenderContext.Instance.SyncScrollPaneToNative(Owner);
        }
    }

    private void UpdatePageController()
    {
        if (_pageController != null && !_pageController.Changing)
        {
            int index;
            if (_scrollType == ScrollType.Horizontal)
                index = CurrentPageX;
            else
                index = CurrentPageY;

            if (index < _pageController.PageCount)
            {
                var c = _pageController;
                _pageController = null;
                c.SelectedIndex = index;
                _pageController = c;
            }
        }
    }

    private void UpdateScrollBars()
    {
        if (_hScrollBar != null)
        {
            _hScrollBar.Visible = _contentWidth > _viewWidth;
        }
        if (_vScrollBar != null)
        {
            _vScrollBar.Visible = _contentHeight > _viewHeight;
        }
    }

    private void OnScrollEnd()
    {
        _scrolling = false;
        Owner?.DispatchEvent("onScrollEnd", null);
    }

    private void KillTween()
    {
        if (_tweener != null)
        {
            _tweener.Kill();
            _tweener = null;
        }
        _tweening = false;
    }

    public void OnTouchBegin(float x, float y)
    {
        if (!_touchEffect) return;
        
        KillTween();
        _dragging = true;
        _isHolding = true;
        _lastTouchPos = new PointF(x, y);
        _lastTouchTime = GetTime();
        _velocityX = 0;
        _velocityY = 0;
    }

    public void OnTouchMove(float x, float y)
    {
        if (!_dragging) return;

        float dx = x - _lastTouchPos.X;
        float dy = y - _lastTouchPos.Y;

        if (_scrollType == ScrollType.Vertical) dx = 0;
        else if (_scrollType == ScrollType.Horizontal) dy = 0;

        float now = GetTime();
        float dt = now - _lastTouchTime;
        if (dt > 0)
        {
            _velocityX = dx / dt * 0.5f + _velocityX * 0.5f;
            _velocityY = dy / dt * 0.5f + _velocityY * 0.5f;
        }
        
        _lastTouchPos = new PointF(x, y);
        _lastTouchTime = now;

        float maxX = Math.Max(0, _contentWidth - _viewWidth);
        float maxY = Math.Max(0, _contentHeight - _viewHeight);

        float newX, newY;
        if (_bouncebackEffect)
        {
            newX = ResistedMove(_xPos, -dx, maxX, _viewWidth);
            newY = ResistedMove(_yPos, -dy, maxY, _viewHeight);
        }
        else
        {
            newX = ClampX(_xPos - dx);
            newY = ClampY(_yPos - dy);
        }

        _xPos = newX;
        _yPos = newY;
        _scrolling = true;
        UpdateScrollPosition();
    }

    public void OnTouchEnd()
    {
        if (!_dragging) return;
        _dragging = false;
        _isHolding = false;

        if (_scrollType == ScrollType.Vertical || _scrollType == ScrollType.Both)
        {
            float max = _contentHeight - _viewHeight;
            if (max <= 0) max = 0;

            if (_yPos < -SCROLL_THRESHOLD)
            {
                _listeners.TryGetValue("onPullDownRelease", out var listener);
                if (listener != null && !listener.IsEmpty)
                    Owner?.DispatchEvent("onPullDownRelease", null);
            }
            else if (_yPos > max + SCROLL_THRESHOLD)
            {
                _listeners.TryGetValue("onPullUpRelease", out var listener);
                if (listener != null && !listener.IsEmpty)
                    Owner?.DispatchEvent("onPullUpRelease", null);
            }
        }
        
        if (_scrollType == ScrollType.Horizontal || _scrollType == ScrollType.Both)
        {
            float max = _contentWidth - _viewWidth;
            if (max <= 0) max = 0;

            if (_xPos < -SCROLL_THRESHOLD)
            {
                _listeners.TryGetValue("onPullDownRelease", out var listener);
                if (listener != null && !listener.IsEmpty)
                    Owner?.DispatchEvent("onPullDownRelease", null);
            }
            else if (_xPos > max + SCROLL_THRESHOLD)
            {
                _listeners.TryGetValue("onPullUpRelease", out var listener);
                if (listener != null && !listener.IsEmpty)
                    Owner?.DispatchEvent("onPullUpRelease", null);
            }
        }

        if (!_inertiaDisabled && (Math.Abs(_velocityX) > INERTIA_VELOCITY_THRESHOLD || Math.Abs(_velocityY) > INERTIA_VELOCITY_THRESHOLD))
        {
            float targetX = _xPos - _velocityX * INERTIA_DISTANCE_FACTOR;
            float targetY = _yPos - _velocityY * INERTIA_DISTANCE_FACTOR;

            if (_pageMode || _snapToItem)
            {
                if (_scrollType == ScrollType.Horizontal && _pageWidth > 0)
                {
                    int page = (int)Math.Round(targetX / _pageWidth);
                    targetX = page * _pageWidth;
                }
                else if (_scrollType == ScrollType.Vertical && _pageHeight > 0)
                {
                    int page = (int)Math.Round(targetY / _pageHeight);
                    targetY = page * _pageHeight;
                }
            }

            SetPos(targetX, targetY, true);
        }
        else
        {
            if (_pageMode || _snapToItem)
            {
                float targetX = _xPos;
                float targetY = _yPos;

                if (_scrollType == ScrollType.Horizontal && _pageWidth > 0)
                {
                    int page = (int)Math.Round(_xPos / _pageWidth);
                    targetX = page * _pageWidth;
                }
                else if (_scrollType == ScrollType.Vertical && _pageHeight > 0)
                {
                    int page = (int)Math.Round(_yPos / _pageHeight);
                    targetY = page * _pageHeight;
                }

                if (targetX != _xPos || targetY != _yPos)
                {
                    SetPos(targetX, targetY, true);
                    return;
                }
            }

            float clampedX = ClampX(_xPos);
            float clampedY = ClampY(_yPos);
            
            float max = _contentHeight - _viewHeight;
            if (max <= 0) max = 0;
            
            if (_headerLockedSize > 0 && clampedY > -_headerLockedSize && clampedY < 0.1f)
                clampedY = -_headerLockedSize;
                
            if (_footerLockedSize > 0 && clampedY < max + _footerLockedSize && clampedY > max - 0.1f)
                clampedY = max + _footerLockedSize;

            if (_bouncebackEffect && (clampedX != _xPos || clampedY != _yPos))
            {
                SetPos(clampedX, clampedY, true);
            }
            else
            {
                OnScrollEnd();
            }
        }
    }

    public void OnMouseWheel(float delta)
    {
        if (!_mouseWheelEnabled) return;
        
        if (_scrollType == ScrollType.Vertical || _scrollType == ScrollType.Both)
        {
            SetPos(_xPos, _yPos - delta * _scrollSpeed * 40, false);
        }
        else
        {
            SetPos(_xPos - delta * _scrollSpeed * 40, _yPos, false);
        }
    }

    private static readonly long _timeBaseMs = Environment.TickCount64;
    private static float GetTime() => (Environment.TickCount64 - _timeBaseMs) / 1000f;

    public void SetContentSize(float width, float height)
    {
        if (_contentWidth != width || _contentHeight != height)
        {
            var oldOverflowX = Math.Max(0f, _contentWidth - _viewWidth);
            var oldOverflowY = Math.Max(0f, _contentHeight - _viewHeight);
            _contentWidth = width;
            _contentHeight = height;
            SetPos(_xPos, _yPos, false);

            if ((_clipSoftnessX > 0 || _clipSoftnessY > 0) && Owner != null)
            {
                Owner.UpdateClipSoftness();
            }

            if (Owner?.NativeObject != null)
            {
                var newOverflowX = Math.Max(0f, _contentWidth - _viewWidth);
                var newOverflowY = Math.Max(0f, _contentHeight - _viewHeight);
                const float epsilon = 0.01f;
                var wasScrollable = oldOverflowX > epsilon || oldOverflowY > epsilon;
                var nowScrollable = newOverflowX > epsilon || newOverflowY > epsilon;
                if (wasScrollable != nowScrollable)
                {
                    SCERenderContext.Instance.UpdateSize(Owner);
                }
            }
        }
    }

    public void SetViewSize(float width, float height)
    {
        if (_viewWidth != width || _viewHeight != height)
        {
            _viewWidth = width;
            _viewHeight = height;
            if (_pageMode)
            {
                _pageWidth = width;
                _pageHeight = height;
            }
            SetPos(_xPos, _yPos, false);

            if ((_clipSoftnessX > 0 || _clipSoftnessY > 0) && Owner != null)
            {
                Owner.UpdateClipSoftness();
            }
        }
    }

    public void Setup(ByteBuffer buffer)
    {
        _scrollType = (ScrollType)buffer.ReadByte();
        var scrollBarDisplay = (ScrollBarDisplayType)buffer.ReadByte();
        int flags = buffer.ReadInt();

        if (buffer.ReadBool())
        {
            _scrollBarMargin = buffer.ReadInt();
            buffer.ReadInt();
            buffer.ReadInt();
            buffer.ReadInt();
        }

        buffer.ReadS();
        buffer.ReadS();
        buffer.ReadS();
        buffer.ReadS();

        _snapToItem = (flags & 2) != 0;
        _pageMode = (flags & 8) != 0;

        if ((flags & 16) != 0)
            _touchEffect = true;
        else if ((flags & 32) != 0)
            _touchEffect = false;

        if ((flags & 64) != 0)
            _bouncebackEffect = true;
        else if ((flags & 128) != 0)
            _bouncebackEffect = false;

        _inertiaDisabled = (flags & 256) != 0;

        if (scrollBarDisplay == ScrollBarDisplayType.Default)
            scrollBarDisplay = ScrollBarDisplayType.Auto;
        _scrollBarDisplayType = scrollBarDisplay;

        if (Owner != null)
        {
            _viewWidth = Owner.Width;
            _viewHeight = Owner.Height;

            if (_pageMode)
            {
                _pageWidth = _viewWidth;
                _pageHeight = _viewHeight;
            }
        }
    }

    public void Dispose()
    {
        KillTween();
        Owner = null;
    }
}
#endif
