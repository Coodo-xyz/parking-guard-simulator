public static class VehiclePathBuilder
{
	public static void Build(
		IReadOnlyList<Vector3> waypoints,
		int samplesPerSegment,
		List<Vector3> path,
		List<float> pathDistances,
		List<float> waypointDistances )
	{
		path.Clear();
		pathDistances.Clear();
		waypointDistances.Clear();

		if ( waypoints.Count == 0 )
		{
			return;
		}

		if ( waypoints.Count == 1 )
		{
			path.Add( waypoints[0].WithZ( 0 ) );
			pathDistances.Add( 0f );
			waypointDistances.Add( 0f );
			return;
		}

		var extended = new List<Vector3>( waypoints.Count + 2 );
		extended.Add( waypoints[0] + (waypoints[0] - waypoints[1]) );
		extended.AddRange( waypoints );
		extended.Add( waypoints[^1] + (waypoints[^1] - waypoints[^2]) );

		var totalDistance = 0f;
		var previous = SampleSegment( extended, 1, 0f ).WithZ( 0 );
		path.Add( previous );
		pathDistances.Add( 0f );

		for ( var segment = 1; segment < extended.Count - 2; segment++ )
		{
			for ( var sample = 1; sample <= samplesPerSegment; sample++ )
			{
				var t = sample / (float)samplesPerSegment;
				var point = SampleSegment( extended, segment, t ).WithZ( 0 );
				totalDistance += (point - previous).Length;
				path.Add( point );
				pathDistances.Add( totalDistance );
				previous = point;
			}
		}

		for ( var i = 0; i < waypoints.Count; i++ )
		{
			waypointDistances.Add( FindClosestDistance( waypoints[i].WithZ( 0 ), path, pathDistances ) );
		}
	}

	static Vector3 SampleSegment( IReadOnlyList<Vector3> points, int segment, float t )
	{
		var p0 = points[segment - 1];
		var p1 = points[segment];
		var p2 = points[segment + 1];
		var p3 = points[segment + 2];
		var tt = t * t;
		var ttt = tt * t;

		return 0.5f * (
			(2f * p1) +
			(-p0 + p2) * t +
			(2f * p0 - 5f * p1 + 4f * p2 - p3) * tt +
			(-p0 + 3f * p1 - 3f * p2 + p3) * ttt
		);
	}

	static float FindClosestDistance( Vector3 target, IReadOnlyList<Vector3> path, IReadOnlyList<float> pathDistances )
	{
		var bestDistance = 0f;
		var bestGap = float.MaxValue;

		for ( var i = 0; i < path.Count; i++ )
		{
			var gap = (path[i] - target).Length;
			if ( gap >= bestGap )
			{
				continue;
			}

			bestGap = gap;
			bestDistance = pathDistances[i];
		}

		return bestDistance;
	}

	public static int FindClosestIndex( Vector3 position, IReadOnlyList<Vector3> path, int startIndex )
	{
		if ( path.Count == 0 )
		{
			return 0;
		}

		if ( startIndex < 0 )
		{
			startIndex = 0;
		}

		if ( startIndex >= path.Count )
		{
			startIndex = path.Count - 1;
		}

		var bestIndex = startIndex;
		var bestGap = (path[startIndex] - position).Length;

		for ( var i = startIndex + 1; i < path.Count; i++ )
		{
			var gap = (path[i] - position).Length;
			if ( gap > bestGap + 2f )
			{
				break;
			}

			if ( gap < bestGap )
			{
				bestGap = gap;
				bestIndex = i;
			}
		}

		return bestIndex;
	}

	public static Vector3 SampleAtDistance(
		float distance,
		IReadOnlyList<Vector3> path,
		IReadOnlyList<float> pathDistances )
	{
		if ( path.Count == 0 )
		{
			return Vector3.Zero;
		}

		if ( distance <= 0f )
		{
			return path[0];
		}

		if ( distance >= pathDistances[^1] )
		{
			return path[^1];
		}

		for ( var i = 1; i < pathDistances.Count; i++ )
		{
			if ( pathDistances[i] < distance )
			{
				continue;
			}

			var span = pathDistances[i] - pathDistances[i - 1];
			if ( span <= 0.001f )
			{
				return path[i];
			}

			var t = (distance - pathDistances[i - 1]) / span;
			return Vector3.Lerp( path[i - 1], path[i], t );
		}

		return path[^1];
	}

	public static float EstimateCurvature(
		float distance,
		IReadOnlyList<Vector3> path,
		IReadOnlyList<float> pathDistances,
		float sampleOffset )
	{
		var a = SampleAtDistance( distance - sampleOffset, path, pathDistances );
		var b = SampleAtDistance( distance, path, pathDistances );
		var c = SampleAtDistance( distance + sampleOffset, path, pathDistances );

		var ab = b - a;
		var bc = c - b;
		ab.z = 0;
		bc.z = 0;

		var lenAb = ab.Length;
		var lenBc = bc.Length;
		if ( lenAb <= 0.01f || lenBc <= 0.01f )
		{
			return 0f;
		}

		ab /= lenAb;
		bc /= lenBc;
		var cross = ab.x * bc.y - ab.y * bc.x;
		var dot = System.MathF.Max( -1f, System.MathF.Min( 1f, Vector3.Dot( ab, bc ) ) );
		var angle = System.MathF.Acos( dot );
		var arcLength = lenAb + lenBc;

		if ( arcLength <= 0.01f )
		{
			return 0f;
		}

		return System.MathF.Abs( angle ) / arcLength * (1f + System.MathF.Abs( cross ) * 0.35f);
	}
}
