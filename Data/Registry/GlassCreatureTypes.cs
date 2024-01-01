#nullable enable
using DreamsOfInfiniteGlass.Character.PlayerCharacter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DreamsOfInfiniteGlass.Data.Registry {
	public static class GlassCreatureTypes {

		/// <summary>
		/// Used to reference the objects by invoking the static constructor.
		/// </summary>
		[MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)] // Force the compiler to keep this here and keep calling it anyway.
		internal static void CallToStaticallyReference() { }

		/// <summary>
		/// A small, metallic spider-like creature that walks along the walls of the structure rooms and cleans/repairs the
		/// world. Any objects it comes nearby will be incinerated/deleted/whatever.
		/// </summary>
		public static readonly CreatureTemplate.Type MAINTAINENCE_SPIDER = new CreatureTemplate.Type("MaintainenceSpider", true);

		/// <summary>
		/// Conservator -- A direct underling to Glass responsible for providing chemical messeges or neural impulses
		/// to other lesser purposed lifeforms. Partly self-aware, but piggybacks off of Glass's intelligence, thus very integrated.
		/// It's a bit like how a GPU is to a CPU; a companion unit that ultimately receives instructions from the main system
		/// but is specialized for offloading certain specific work.
		/// </summary>
		public static readonly CreatureTemplate.Type CONSERVATOR = new CreatureTemplate.Type("Conservator", true);

		/// <summary>
		/// Progenitor -- A bit like an inspector, but modified to operate systems that grow new purposed lifeforms. It guards
		/// the lifepods it lives around and becomes very uneasy as the player approaches any of its work area.
		/// </summary>
		public static readonly CreatureTemplate.Type PROGENITOR = new CreatureTemplate.Type("Progenitor", true);

		/// <summary>
		/// Sentry -- Also a bit like an inspector, but modified to be able to come into direct contact with Void Fluid with little
		/// consequence. It is responsible for inspecting the void pipeline, spotting leaks or weaknesses, and repairing them.
		/// </summary>
		public static readonly CreatureTemplate.Type SENTRY = new CreatureTemplate.Type("Sentry", true);

		/// <summary>
		/// <see cref="MechPlayer"/> enters a part of the facility they are not supposed to be in.
		/// This part of the facility would contain Progenitors or Sentries.
		/// </summary>
		public static readonly CreatureTemplate.Relationship.Type RELATIONSHIP_TYPE_UNEASY_POSITIVE = new CreatureTemplate.Relationship.Type("GlassUneasyPositive", true);

		/// <summary>
		/// The same as <see cref="RELATIONSHIP_TYPE_UNEASY_POSITIVE"/>, but for the Conservator, who is not particularly 
		/// happy with the <see cref="MechPlayer"/> deviating from their purpose and exploring.
		/// <para/>
		/// Still, the feelings are not negative, just alert. 
		/// </summary>
		public static readonly CreatureTemplate.Relationship.Type RELATIONSHIP_TYPE_UNAUTHORIZED = new CreatureTemplate.Relationship.Type("GlassUnauthorized", true);

		/// <summary>
		/// Construct <see cref="RELATIONSHIP_TYPE_UNEASY_POSITIVE"/> with an intensity of 100%.
		/// </summary>
		public static CreatureTemplate.Relationship RELATIONSHIP_UNEASY_POSITIVE => new CreatureTemplate.Relationship(RELATIONSHIP_TYPE_UNEASY_POSITIVE, 1.0f);

		/// <summary>
		/// Construct <see cref="RELATIONSHIP_TYPE_UNAUTHORIZED"/> with an intensity of 100%.
		/// </summary>
		public static CreatureTemplate.Relationship RELATIONSHIP_UNAUTHORIZED => new CreatureTemplate.Relationship(RELATIONSHIP_TYPE_UNAUTHORIZED, 1.0f);

		// TODO: Inspector as ancestor for Progenitor/Sentry?
		public static readonly CreatureTemplate CONSERVATOR_TEMPLATE = new CreatureTemplate(CONSERVATOR, null, new List<TileTypeResistance>(), new List<TileConnectionResistance>(), RELATIONSHIP_UNAUTHORIZED);
		public static readonly CreatureTemplate PROGENITOR_TEMPLATE = new CreatureTemplate(PROGENITOR, null, new List<TileTypeResistance>(), new List<TileConnectionResistance>(), RELATIONSHIP_UNEASY_POSITIVE);
		public static readonly CreatureTemplate SENTRY_TEMPLATE = new CreatureTemplate(SENTRY, null, new List<TileTypeResistance>(), new List<TileConnectionResistance>(), RELATIONSHIP_UNEASY_POSITIVE);

	}
}
