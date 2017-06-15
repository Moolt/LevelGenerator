using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

[Serializable]
public class FuzzyAttributes{
	public FuzzyAttribute[] attributes;
}

[Serializable]
public class FuzzyAttribute{
	public string name;
	public string[] attributes;

	public FuzzyDescriptor[] descriptors;
}

[Serializable]
public class FuzzyDescriptor{
	public string name;
	public float value;
}

public enum TagType{ GENERATED, USER };

public static class FuzzyTagDictionary{

	public static Dictionary<string, List<FuzzyDescriptor>> mapping;
	private static string oldFile;

	private static void LoadDictionary(){
		TextAsset textAsset = Resources.Load ("ProceduralGeneration/DynamicTags") as TextAsset;
		if (oldFile != textAsset.text) {
			oldFile = textAsset.text;
			FuzzyAttributes attributes = JsonUtility.FromJson<FuzzyAttributes> (textAsset.text);
			mapping = new Dictionary<string, List<FuzzyDescriptor>> ();
			foreach (FuzzyAttribute attribute in attributes.attributes) {
				List<FuzzyDescriptor> descriptors = attribute.descriptors.OrderBy (d => d.value).ToList ();
				descriptors [descriptors.Count - 1].value = 1f;
				mapping.Add (attribute.name, descriptors);
			}
		}
	}

	public static Dictionary<string, List<FuzzyDescriptor>> Mapping{
		get{
			LoadDictionary ();
			return mapping;
		}
	}

	public static TagInstance FindTag(string attributeName, float value){
		List<FuzzyDescriptor> tags = Mapping [attributeName];
		int index = 0;

		for (int i = 0; i < tags.Count; i++) {
			if (value <= tags [i].value) {
				index = i;
				break;
			}
		}
		return new TagInstance (attributeName, TagType.GENERATED, tags [index].name, value);
	}

	public static string[] Descriptors{
		get{			
			return Mapping.Select (x => x.Key).ToArray();
		}
	}

	public static string FindAttribute(int index, float value){
		return FindTag (Descriptors [index], value).Name;
	}
}

public class EnemyTag : DynamicTag{
	protected override float InterpolationValue(){
		int amount = GameObject.FindGameObjectsWithTag ("Enemy").Length;
		float smallestValue = 0;
		float highestValue = 10;
		return Mathf.InverseLerp (smallestValue, highestValue, amount);
	}
}

[Serializable]
public struct TagInstance{
	public string Descriptor;
	public TagType Type;
	public string Name;
	public float Value;

	public TagInstance (string descriptor, TagType type, string name, float value){
		this.Descriptor = descriptor;
		this.Type = type;
		this.Name = name;
		this.Value = value;
	}

	public override bool Equals (object obj){
		if (obj == null)
			return false;
		if (ReferenceEquals (this, obj))
			return true;
		if (obj.GetType () != typeof(TagInstance))
			return false;
		TagInstance other = (TagInstance)obj;
		return Name == other.Name;
	}


	public override int GetHashCode (){
		unchecked {
			return (Name != null ? Name.GetHashCode () : 0);
		}
	}
}

public class LightingTag : DynamicTag{
	protected override float InterpolationValue(){
		float area = bounds.Size.x * bounds.Size.z;
		float val = area;

		Light[] lights = chunk.GetComponentsInChildren<Light> ();

		foreach (Light light in lights) {
			val -= (Mathf.PI * ((light.range) * (light.range))) * (light.intensity / 10f);
		}

		val = area - Mathf.Max (val, 0f);

		float minValue = 0f;
		float maxValue = area;
		return Mathf.InverseLerp (minValue, maxValue, val);
	}
}

public class InstantiationTag : DynamicTag{
	protected override float InterpolationValue(){
		float val = 0f;
		List<AbstractProperty> properties = chunk.GetComponentsInChildren<AbstractProperty> ().ToList();
		properties.OfType<AbstractMesh> ().ToList ().ForEach (am => val += .2f);
		properties.OfType<LinearArray> ().ToList ().ForEach (la => val += la.duplicateCount * .1f);
		properties.OfType<ScatteredArray> ().ToList ().ForEach (sa => val += sa.CalculatedCount * 0.2f);
		properties.OfType<WildcardAsset> ().ToList ().ForEach (sa => val += 0.5f);
		properties.OfType<AbstractValue> ().ToList ().ForEach (sa => val += 0.15f);
		properties.OfType<AbstractBounds> ().ToList ().ForEach (sa => val += 0.1f);
		float minValue = 0f;
		float maxValue = 10f;
		return Mathf.InverseLerp (minValue, maxValue, val);
	}
}

public class VariabilityTag : DynamicTag{
	protected override float InterpolationValue(){
		float val = 0f;
		bool isBoundsStatic = bounds.hasFixedSize;
		List<AbstractProperty> ownProperties = chunk.GetComponents<AbstractProperty> ().ToList();
		List<AbstractBounds> childrenAbstractBounds = chunk.GetComponentsInChildren<AbstractBounds> ().ToList();
		List<AbstractProperty> childProperties = chunk.GetComponentsInChildren<AbstractProperty> ().ToList();
		ownProperties.ForEach (op => childProperties.Remove (op));
		childrenAbstractBounds.ForEach (cab => childProperties.Remove (cab));

		bool hasDynamicComponents = childProperties.Count > 0;
		childrenAbstractBounds.ForEach (cab => hasDynamicComponents |= !cab.hasFixedSize);

		val += isBoundsStatic ? 0f : .5f;
		val += hasDynamicComponents ? .5f : 0f;

		float minValue = 0f;
		float maxValue = 1f;
		return Mathf.InverseLerp (minValue, maxValue, val);
	}
}

public class DetailTag : DynamicTag{
	protected override float InterpolationValue(){
		int objectsCount = chunk.transform.childCount;

		List<LinearArray> linearArrays = chunk.GetComponentsInChildren<LinearArray> ().ToList();
		List<ScatteredArray> scatteredArrays = chunk.GetComponentsInChildren<ScatteredArray> ().ToList();
		linearArrays.ForEach (la => objectsCount += la.duplicateCount);
		scatteredArrays.ForEach (sa => objectsCount += sa.CalculatedCount);
		float area = new Vector2 (bounds.Size.x, bounds.Size.z).magnitude;
		float minValue = 0f;
		float maxValue = 3f;
		float val = objectsCount / area;
		return Mathf.InverseLerp (minValue, maxValue, val);
	}
}

public class RoomTypeTag : DynamicTag{
	protected override float InterpolationValue(){
		DoorManager doorManager = chunk.GetComponent<DoorManager> () as DoorManager;
		int doorCount = doorManager.doors.Count;
		float minValue = 1f;
		float maxValue = 4f;
		return Mathf.InverseLerp (minValue, maxValue, doorCount);
	}
}

public class AspectRatioTag : DynamicTag{
	protected override float InterpolationValue(){
		float minWidth = Mathf.Min (bounds.Size.x, bounds.Size.z);
		float maxWidth = Mathf.Max (bounds.Size.x, bounds.Size.z);
		float val = minWidth / maxWidth;
		return Mathf.Clamp (val, 0f, 1f);
	}
}

public class AbsoluteHeightTag : DynamicTag{
	protected override float InterpolationValue(){
		float minValue = 3f;
		float maxValue = 18f;
		float height = bounds.Size.y;
		return Mathf.InverseLerp (minValue, maxValue, height);
	}
}

public class RelativeHeightTag : DynamicTag{
	protected override float InterpolationValue(){
		float minValue = .2f;
		float maxValue = 1.5f;
		float baseSize = Mathf.Min (bounds.Size.x, bounds.Size.z);
		float height = bounds.Size.y;
		float relHeight = Mathf.Max (height / baseSize, 0f);
		return Mathf.InverseLerp (minValue, maxValue, relHeight);
	}
}

public class SizeTag : DynamicTag{
	protected override float InterpolationValue(){
		float minValue = 12f;
		float maxValue = 45f;
		float size = bounds.Size.magnitude;
		return Mathf.InverseLerp (minValue, maxValue, size);
	}
}

public abstract class DynamicTag{
	protected static AbstractBounds bounds;
	protected static GameObject chunk;
	private static DynamicTag instance;

	protected abstract float InterpolationValue();

	public static GameObject Chunk {
		get {
			return chunk;
		}
		set {
			chunk = value;
			if (chunk != null) {
				bounds = chunk.GetComponent<AbstractBounds> ();
			}
		}
	}

	public TagInstance ObtainTag(){
		return FuzzyTagDictionary.FindTag (Descriptor, InterpolationValue ());
	}

	public string Descriptor {
		get { return this.GetType ().ToString ().Replace ("Tag", ""); }
	}
}

public enum TagTarget{ HALLWAY, CHUNK }

[ExecuteInEditMode]
public class ChunkTags : TagProperty {

	public List<TagInstance> userGenerated = new List<TagInstance>();
	public List<TagInstance> autoGenerated = new List<TagInstance>();
	private bool autoUpdate = false;

	public void OnEnable(){
		DynamicTag.Chunk = GameObject.FindGameObjectWithTag ("Chunk");
	}

	public override void Preview(){
		if (autoUpdate) {
			UpdateTags ();
		}
	}

	public override void Generate(){
		//UpdateTags ();
	}

	public void UpdateTags(){
		autoGenerated.Clear ();
		List<Type> types = typeof(DynamicTag).Assembly.GetTypes ().Where (type => type.IsSubclassOf (typeof(DynamicTag))).ToList();

		foreach (Type type in types) {
			DynamicTag dynTag = (DynamicTag)Activator.CreateInstance (type);
			autoGenerated.Add (dynTag.ObtainTag ());
		}
	}

	public List<TagInstance> Tags{
		get{
			List<TagInstance> allTags = new List<TagInstance> ();
			allTags.AddRange (userGenerated);
			allTags.AddRange (autoGenerated);
			return allTags;
		}
	}

	public bool AutoUpdate {
		get {
			return this.autoUpdate;
		}
		set {
			autoUpdate = value;
		}
	}

	public override RemovalTime RemovalTime{
		get { return RemovalTime.MANUAL; }
	}

	//Returns a list of all user defined tags without duplicates
	public static string[] GlobalUserTags(TagTarget _target){
		string path = _target == TagTarget.CHUNK ? GlobalPaths.RelativeChunkPath : GlobalPaths.RelativeHallwayPath;
		List<string> globalUserTags = new List<string> ();
		List<GameObject> chunks = Resources.LoadAll<GameObject> (path).ToList();
		List<ChunkTags> chunkTags = new List<ChunkTags> ();

		chunks.Where (c => c.GetComponentInChildren<ChunkTags> () != null)
			.ToList ()
			.ForEach (c => chunkTags.Add (c.GetComponentInChildren<ChunkTags> ()));

		chunkTags.SelectMany (ct => ct.userGenerated)
			.Where (t => !globalUserTags.Contains (t.Name))
			.ToList ()
			.ForEach (t => globalUserTags.Add (t.Name));

		return globalUserTags.ToArray ();
	}
}
