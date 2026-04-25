using System;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalSmoothScrollList : UIList
{
    private const float SmoothTimeSeconds = 0.11f;
    private const float SnapDistance = 0.035f;
    private const float SnapVelocity = 0.01f;
    private const float MaxDeltaTime = 1f / 30f;

    private bool _initialized;
    private bool _isAnimating;

    private float _visualViewPosition;
    private float _targetViewPosition;
    private float _scrollVelocity;

    public override void ScrollWheel(UIScrollWheelEvent evt)
    {
        EnsureInitialized();

        var visualViewPosition = _visualViewPosition;

        ViewPosition = _targetViewPosition;
        base.ScrollWheel(evt);

        _targetViewPosition = ViewPosition;
        _visualViewPosition = visualViewPosition;

        ViewPosition = _visualViewPosition;

        _isAnimating = MathF.Abs(_targetViewPosition - _visualViewPosition) > SnapDistance;

        if (!_isAnimating)
        {
            SnapToTarget();
        }
    }

    public override void Update(GameTime gameTime)
    {
        EnsureInitialized();

        if (_isAnimating)
        {
            var deltaTime = MathHelper.Clamp(
                (float)gameTime.ElapsedGameTime.TotalSeconds,
                0f,
                MaxDeltaTime
            );

            _visualViewPosition = SmoothDamp(
                _visualViewPosition,
                _targetViewPosition,
                ref _scrollVelocity,
                SmoothTimeSeconds,
                deltaTime
            );

            if (MathF.Abs(_targetViewPosition - _visualViewPosition) <= SnapDistance &&
                MathF.Abs(_scrollVelocity) <= SnapVelocity)
            {
                SnapToTarget();
            }
            else
            {
                ViewPosition = _visualViewPosition;
            }
        }
        else
        {
            _visualViewPosition = ViewPosition;
            _targetViewPosition = ViewPosition;
            _scrollVelocity = 0f;
        }

        base.Update(gameTime);
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        _visualViewPosition = ViewPosition;
        _targetViewPosition = ViewPosition;
        _scrollVelocity = 0f;
        _initialized = true;
    }

    private void SnapToTarget()
    {
        _visualViewPosition = _targetViewPosition;
        _scrollVelocity = 0f;
        _isAnimating = false;
        ViewPosition = _targetViewPosition;
    }

    private static float SmoothDamp(
        float current,
        float target,
        ref float currentVelocity,
        float smoothTime,
        float deltaTime
    )
    {
        if (deltaTime <= 0f)
            return current;

        smoothTime = MathF.Max(0.0001f, smoothTime);

        var omega = 2f / smoothTime;
        var x = omega * deltaTime;
        var exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);

        var originalTarget = target;
        var change = current - target;

        target = current - change;

        var temp = (currentVelocity + omega * change) * deltaTime;
        currentVelocity = (currentVelocity - omega * temp) * exp;

        var output = target + (change + temp) * exp;

        if ((originalTarget - current > 0f) != (output > originalTarget)) return output;
        output = originalTarget;
        currentVelocity = 0f;

        return output;
    }
}
