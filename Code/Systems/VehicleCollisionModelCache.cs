public static class VehicleCollisionModelCache
{
	static readonly Dictionary<string, Model> cache = new();

	public static Model GetCollisionModel( Model visualModel )
	{
		if ( visualModel == null || !visualModel.IsValid )
		{
			return null;
		}

		if ( HasPhysicsShapes( visualModel ) )
		{
			return visualModel;
		}

		var key = visualModel.Name;
		if ( cache.TryGetValue( key, out var cached ) && cached != null && cached.IsValid )
		{
			return cached;
		}

		var collisionModel = BuildFromRenderMesh( visualModel );
		if ( collisionModel != null && collisionModel.IsValid )
		{
			cache[key] = collisionModel;
		}

		return collisionModel;
	}

	public static bool HasPhysicsShapes( Model model )
	{
		var size = model.PhysicsBounds.Size;
		return size.x > 1f && size.y > 1f && size.z > 1f;
	}

	static Model BuildFromRenderMesh( Model visualModel )
	{
		var vertices = visualModel.GetVertices();
		var indices = visualModel.GetIndices();

		if ( vertices != null && vertices.Length > 0 )
		{
			var positions = new List<Vector3>( vertices.Length );
			foreach ( var vertex in vertices )
			{
				positions.Add( vertex.Position );
			}

			var builder = Model.Builder.WithName( $"{visualModel.Name}_collision_hull" );
			builder.AddCollisionHull( positions );
			var hullModel = builder.Create();
			if ( hullModel != null && hullModel.IsValid && HasPhysicsShapes( hullModel ) )
			{
				return hullModel;
			}
		}

		if ( indices != null && indices.Length > 0 && vertices != null && vertices.Length > 0 )
		{
			var positions = new List<Vector3>( vertices.Length );
			foreach ( var vertex in vertices )
			{
				positions.Add( vertex.Position );
			}

			var indexList = new List<int>( indices.Length );
			foreach ( var index in indices )
			{
				indexList.Add( (int)index );
			}

			var meshModel = Model.Builder
				.WithName( $"{visualModel.Name}_collision_mesh" )
				.AddCollisionMesh( positions, indexList )
				.Create();

			if ( meshModel != null && meshModel.IsValid && HasPhysicsShapes( meshModel ) )
			{
				return meshModel;
			}
		}

		return BuildFromBounds( visualModel );
	}

	static Model BuildFromBounds( Model visualModel )
	{
		var bounds = visualModel.Bounds;
		return Model.Builder
			.WithName( $"{visualModel.Name}_collision_bounds" )
			.AddCollisionBox( bounds.Size * 0.5f, bounds.Center )
			.Create();
	}
}
