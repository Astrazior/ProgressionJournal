using Microsoft.Xna.Framework;

namespace ProgressionJournal.UI.Controls;

internal sealed class JournalSmoothScrollController
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

    public float BeginScroll(float currentViewPosition)
    {
        EnsureInitialized(currentViewPosition);
        return _targetViewPosition;
    }

    public float EndScroll(float targetViewPosition)
    {
        _targetViewPosition = targetViewPosition;
        _isAnimating = MathF.Abs(_targetViewPosition - _visualViewPosition) > SnapDistance;
        if (!_isAnimating)
        {
            SnapToTarget();
        }

        return _visualViewPosition;
    }

    public float Update(GameTime gameTime, float currentViewPosition)
    {
        EnsureInitialized(currentViewPosition);

        if (!_isAnimating)
        {
            _visualViewPosition = currentViewPosition;
            _targetViewPosition = currentViewPosition;
            _scrollVelocity = 0f;
            return currentViewPosition;
        }

        var deltaTime = MathHelper.Clamp(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            0f,
            MaxDeltaTime);
        _visualViewPosition = SmoothDamp(
            _visualViewPosition,
            _targetViewPosition,
            ref _scrollVelocity,
            SmoothTimeSeconds,
            deltaTime);

        if (MathF.Abs(_targetViewPosition - _visualViewPosition) <= SnapDistance
            && MathF.Abs(_scrollVelocity) <= SnapVelocity)
        {
            SnapToTarget();
        }

        return _visualViewPosition;
    }

    private void EnsureInitialized(float viewPosition)
    {
        if (_initialized)
        {
            return;
        }

        _visualViewPosition = viewPosition;
        _targetViewPosition = viewPosition;
        _scrollVelocity = 0f;
        _initialized = true;
    }

    private void SnapToTarget()
    {
        _visualViewPosition = _targetViewPosition;
        _scrollVelocity = 0f;
        _isAnimating = false;
    }

    private static float SmoothDamp(
        float current,
        float target,
        ref float currentVelocity,
        float smoothTime,
        float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return current;
        }

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

        if ((originalTarget - current > 0f) != (output > originalTarget))
        {
            return output;
        }

        currentVelocity = 0f;
        return originalTarget;
    }
}
