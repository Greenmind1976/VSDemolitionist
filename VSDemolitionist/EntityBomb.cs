using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace VSDemolitionist;

public class EntityBomb : Entity
{
    private bool hasImpactedGround = false;
    private double prevMotionX;
    private double prevMotionZ;

    private double rollDistance = 0;
    private const double maxRollDistance = 3.0; // 2–3 blocks

    public override void OnGameTick(float dt)
    {
        base.OnGameTick(dt);

        // -----------------------
        // Airborne spin
        // -----------------------
        if (!this.OnGround)
        {
            this.ServerPos.Yaw += 720f * dt;
            return;
        }

        // -----------------------
        // First ground contact
        // -----------------------
        if (this.OnGround && !hasImpactedGround)
        {
            hasImpactedGround = true;

            // Small bounce
            this.ServerPos.Motion.Y *= -0.15;

            if (System.Math.Abs(this.ServerPos.Motion.Y) < 0.05)
            {
                this.ServerPos.Motion.Y = 0;
            }
        }

        // -----------------------
        // Detect wall impact
        // -----------------------
        bool hitWallX = System.Math.Abs(prevMotionX) > 0.05 && System.Math.Abs(this.ServerPos.Motion.X) < 0.01;
        bool hitWallZ = System.Math.Abs(prevMotionZ) > 0.05 && System.Math.Abs(this.ServerPos.Motion.Z) < 0.01;

        if (hitWallX || hitWallZ)
        {
            // Stop completely if we hit something solid
            this.ServerPos.Motion.X = 0;
            this.ServerPos.Motion.Z = 0;
            return;
        }

        // -----------------------
        // Track roll distance
        // -----------------------
        double horizontalSpeed = System.Math.Sqrt(
            this.ServerPos.Motion.X * this.ServerPos.Motion.X +
            this.ServerPos.Motion.Z * this.ServerPos.Motion.Z
        );

        rollDistance += horizontalSpeed * dt;

        // Fake rolling spin
        if (horizontalSpeed > 0.01)
        {
            this.ServerPos.Yaw += 360f * dt;
        }

        // Stop if rolled too far
        if (rollDistance >= maxRollDistance)
        {
            this.ServerPos.Motion.X = 0;
            this.ServerPos.Motion.Z = 0;
            return;
        }

        // Mild friction while rolling
        double friction = 1.0 - (2.0 * dt);
        if (friction < 0) friction = 0;

        this.ServerPos.Motion.X *= friction;
        this.ServerPos.Motion.Z *= friction;

        prevMotionX = this.ServerPos.Motion.X;
        prevMotionZ = this.ServerPos.Motion.Z;
    }
}