public static class ModelColliderUtility
{
	public static BoxCollider EnsureBox( Component owner, Vector3 scale, Vector3 center, bool isStatic )
	{
		var modelCollider = owner.Components.Get<ModelCollider>();
		if ( modelCollider != null )
		{
			modelCollider.Destroy();
		}

		var collider = owner.Components.Get<BoxCollider>();
		if ( collider == null )
		{
			collider = owner.Components.Create<BoxCollider>();
		}

		collider.Scale = scale;
		collider.Center = center;
		collider.Static = isStatic;
		collider.IsTrigger = false;
		EnsureRigidbody( owner, isStatic );
		return collider;
	}

	public static Collider EnsureModelOrBox(
		Component owner,
		Model model,
		Vector3 fallbackScale,
		Vector3 fallbackCenter,
		bool isStatic )
	{
		if ( model != null )
		{
			var modelCollider = owner.Components.Get<ModelCollider>();
			if ( modelCollider == null )
			{
				modelCollider = owner.Components.Create<ModelCollider>();
			}

			var box = owner.Components.Get<BoxCollider>();
			if ( box != null )
			{
				box.Destroy();
			}

			modelCollider.Model = model;
			modelCollider.Static = isStatic;
			modelCollider.IsTrigger = false;

			if ( VehicleCollisionModelCache.HasPhysicsShapes( model ) )
			{
				EnsureRigidbody( owner, isStatic );
				return modelCollider;
			}

			modelCollider.Destroy();
		}

		return EnsureBox( owner, fallbackScale, fallbackCenter, isStatic );
	}

	static void EnsureRigidbody( Component owner, bool isStatic )
	{
		if ( isStatic )
		{
			var rigidbody = owner.Components.Get<Rigidbody>();
			if ( rigidbody != null )
			{
				rigidbody.Destroy();
			}

			return;
		}

		var body = owner.Components.Get<Rigidbody>();
		if ( body == null )
		{
			body = owner.Components.Create<Rigidbody>();
		}

		body.Gravity = false;
		body.MotionEnabled = false;

		if ( body.PhysicsBody != null )
		{
			body.PhysicsBody.UseController = true;
		}
	}

	public static void SyncTransform( Component owner, Vector3 position, Rotation rotation )
	{
		var rigidbody = owner.Components.Get<Rigidbody>();
		var physicsBody = rigidbody?.PhysicsBody;
		if ( physicsBody == null )
		{
			return;
		}

		physicsBody.UseController = true;
		physicsBody.Position = position;
		physicsBody.Rotation = rotation;
	}
}
