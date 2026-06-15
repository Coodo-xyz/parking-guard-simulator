using Sandbox;

public sealed class PopularitySystem : Component
{
	[Property, ReadOnly] public float Popularity { get; private set; } = 50f;

	public void Increase( float amount ) => Popularity = Math.Clamp( Popularity + amount, 0, 100 );
	public void Decrease( float amount ) => Popularity = Math.Clamp( Popularity - amount, 0, 100 );

	/// <summary>Higher popularity = more vehicles spawn per minute</summary>
	public float VehicleSpawnRate => MathX.Lerp( 0.2f, 2.0f, Popularity / 100f );
}
