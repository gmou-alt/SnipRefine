using System;

namespace SnapGrab
{
    // Exact critically damped spring, measured in seconds; independent of timer rate.
    internal sealed class SidebarMotion
    {
        internal double Position { get; private set; }
        internal double Velocity { get; private set; }
        internal double Target { get; private set; }
        internal bool Moving { get { return Position != Target || Velocity != 0; } }

        internal void Jump(double value)
        {
            Position = Target = Clamp(value);
            Velocity = 0;
        }

        internal void MoveTo(double target) { Target = Clamp(target); }

        internal double Advance(double seconds)
        {
            if (seconds <= 0 || double.IsNaN(seconds) || double.IsInfinity(seconds)) return Position;
            const double frequency = 24;
            double offset = Position - Target;
            double term = Velocity + frequency * offset;
            double decay = Math.Exp(-frequency * seconds);
            Position = Target + (offset + term * seconds) * decay;
            Velocity = (Velocity - frequency * term * seconds) * decay;
            if (Position < 0 || Position > 1) { Position = Clamp(Position); Velocity = 0; }
            if (Math.Abs(Position - Target) < 0.0006 && Math.Abs(Velocity) < 0.02)
                Jump(Target);
            return Position;
        }

        private static double Clamp(double value) { return Math.Max(0, Math.Min(1, value)); }
    }
}
