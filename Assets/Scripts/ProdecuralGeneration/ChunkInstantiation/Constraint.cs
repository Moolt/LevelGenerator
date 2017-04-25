using System.Collections.Generic;
using System.Xml.Serialization;
using System.Collections;
using UnityEngine;
using System.Linq;
using System;

//FuzzyProperties = Automatic generated Properties that represent a value from 0 to 1 and have descriptive attributes
//UserDefinedTags = Simple strings that the user can define
public enum ConstraintType{ FuzzyProperties, UserDefinedTags }
//The target rooms that the constraint applies to. Can be further specified by the ConstraintAmount
public enum ConstraintTarget{ AllRooms, StartRoom, EndRoom, MiddleRooms, SideRooms, Hallways, Disabled}
//The ConstraintTarget is further narrowed down with the ConstraintAmount
public enum ConstraintAmount{ All, AtLeast, AtMost, Exactly, None}
//Relative Amount of segments to be used by Hallway Templates
public enum HallwayAmount{ Unconstrained, WithTag, WithoutTag, SetPriority, AtMost }
public enum TagComparisonMethod{ And, Or }
//Absolute amount of rooms or relative/percentual amount (relative to the total amount of rooms)
public enum ConstraintAmountType{ Absolute, Percentual }

[Serializable]
public class Constraint {
	protected ConstraintAmountType amountType;
	protected TagComparisonMethod tagMethod;
	protected ConstraintTarget target;
	protected ConstraintAmount amount;
	protected HallwayAmount hAmount;
	protected ConstraintType type;
	protected List<string> parsedTags; //Parsed from the rawTags by separating the string by semicolons
	protected string fuzzyTagName; //The name of the selected fuzzy property
	protected float relativeAmount; //Relative amount of rooms that are affected by the constraint
	protected int absoluteAmount; //Absolute amount, see above
	protected int autoTagIndex; //Used by the GUI to store the index of the fuzzyTag in the ChunkTags list
	protected string rawTags; //The raw string of user definied tags, separated by semicolons
	protected float hallwayPriority = 0f; //Decides which segment is used
	public float Min = 0f; //The minimal value of the fuzzyTag value interval
	public float Max = 1f; //Maximal value, see above
	//Only used during instantiation and later reset
	private int matchingChunksUsed = 0; //Stores the amount of chunks that matched this constraint and were actually used in the level
	private int matchingHallwaysUsed = 0;
	private int atMostAmount = -1; //Only relevant inf ConstraintAmount = atMost. Stores a (random) number of rooms, that are allowed to match this constraint

	public Constraint(){
		parsedTags = new List<string> ();
		rawTags = "";
	}

	//Used only for Chunks by the ChunkHelper Class
	public bool IsConstraintSatisfied(ChunkMetadata meta, ChunkHelperProgress progress){
		ChunkTags chunkTags = meta.Chunk.GetComponent<ChunkTags> ();
		bool result = ConstraintResult (chunkTags);

		if (amount == ConstraintAmount.None) {
			//Eventhough the amount is none, we don't want to return false
			//If the result was true, meaning the constraint applies to this chunk, we want to negate it to false
			//This will result in the chunk not being used. If the result was false before, this means that this constraint
			//Doesn't apply to the given chunk, meaning, that we don't want to affect it's creation by returning false, thus returning true
			result = !result;
		} else if (amount != ConstraintAmount.All) {
			result = HandleRoomAmount (meta, progress, result);
		}

		return result;
	}

	public bool IsConstraintSatisfied(HallwayTemplateMeta meta){
		type = ConstraintType.UserDefinedTags; //Never actually set by the GUI
		bool result = ConstraintResult (meta.tags);
	
		switch (hAmount) {
		case HallwayAmount.WithTag:
			break; //The result is already the one we want, only true if tags are the same
		case HallwayAmount.WithoutTag:
			result = !result;
			break;
		case HallwayAmount.Unconstrained:
			result = true;
			break;
		case HallwayAmount.AtMost:
			//Either the constraint doesn't apply to this segment or when it does
			//Check whether the amount is within bounds
			if (result) {
				meta.RegisterAtMostConstraint (this);
			}
			result = !result || (result && matchingHallwaysUsed < absoluteAmount);
			break;
		case HallwayAmount.SetPriority:
			result = true;
			break;
		}

		return result;
	}

	public void CalculateHallwayPriority(HallwayTemplateMeta meta){
		type = ConstraintType.UserDefinedTags; //Never actually set by the GUI
		bool result = ConstraintResult (meta.tags);
		if (result && hAmount == HallwayAmount.SetPriority) {
			meta.ConstraintPriority += hallwayPriority;
		}
	}

	//Other than IsConstraintSatisfied, this method depends on the current context of the chunk generation
	//Returns true, if the ConstraintAmount has not YET been satisfied
	public bool HandleRoomAmount(ChunkMetadata meta, ChunkHelperProgress progress, bool previousResult){
		float remaining = progress.Remaining (target) + 0.0001f; //Remaining rooms that need be created
		float priorityBonus = (absoluteAmount / remaining); //A chunk gets a priority bonus if the constrain is satisfied and not all chunks have been created
		int wantedAmount = GetAmountValue (progress.TotalRoomCount (target));

		bool enoughCreated = matchingChunksUsed >= wantedAmount;
		meta.Priority += previousResult && !enoughCreated ? priorityBonus: 0f;

		if (previousResult) {
			meta.RegisterAsMatching (this); //Registers this constraint at the chunkmeta and will be notified, if the chunk has been created
		}

		bool stillSatisfied = true;

		switch (amount) {
		case ConstraintAmount.AtLeast:
			stillSatisfied = true; //Allways true, since there is no upper limit
			break;
		case ConstraintAmount.Exactly: //Exactly and AtMost are handled identically.
		case ConstraintAmount.AtMost: //The upper amount for AtMost is randomly specified in the GetAmountMethod
			stillSatisfied = !previousResult || (previousResult && !enoughCreated);
			break;
		}

		return stillSatisfied;
	}

	//The amount of chunks affected by this constraint
	//May have to be calculated in the cases of "atMost" or a relative amount of chunks
	private int GetAmountValue(int totalRoomCount){
		int _amountValue;
		if(amountType == ConstraintAmountType.Absolute){
			_amountValue = absoluteAmount;
		}  else{
			_amountValue = (int)Mathf.Round (relativeAmount * totalRoomCount);
		}
		if (amount == ConstraintAmount.AtMost) {
			if (atMostAmount == -1) {
				atMostAmount = (int)Mathf.Round (_amountValue * UnityEngine.Random.value);
			}
			_amountValue = atMostAmount;
		}
		return _amountValue;
	}

	//Called by a ChunkMeta instance in case it's chunk has been used
	//A constraint has to register to the ChunkMeta in order to receive this update
	public void MatchingChunkHasBeendUsed(){
		matchingChunksUsed++;
	}

	public void MatchingHallwayHasBeenUsed(){
		matchingHallwaysUsed++;
	}

	//Returns, wether a constraint has beend satisfied.
	//Detects whether this is a user or fuzzy tag
	private bool ConstraintResult(ChunkTags chunkTags){
		bool result;
		if (type == ConstraintType.FuzzyProperties) {			
			result = IsFuzzySatisfied (chunkTags);
		} else {
			result = IsUserTagsSatisfied (chunkTags);
		}
		return result;
	}

	//Used to check for all masks that are effected by this constraint
	public bool IsAffectedByConstraints(ChunkTags tags){
		return ConstraintResult (tags);
	}
		
	private bool IsFuzzySatisfied (ChunkTags chunkTags){
		if (chunkTags != null) {
			List<TagInstance> autoTags = chunkTags.autoGenerated;
			//Find the tag instance of the chunk with by the name defined in the GUI
			//It's value is then compared with min and max
			TagInstance toTest = autoTags.Find (ti => ti.Descriptor == fuzzyTagName);
			return toTest.Value <= Max && toTest.Value >= Min;
		}
		return true;
	}

	private bool IsUserTagsSatisfied (ChunkTags chunkTags){
		IEnumerable<string> userTags = chunkTags.userGenerated.Select(ct => ct.Name);
		if (tagMethod == TagComparisonMethod.And) {
			return parsedTags.All (pt => userTags.Contains (pt));
		} else {
			return parsedTags.Any (pt => userTags.Contains (pt));
		}
	}

	public ConstraintType Type {
		get {
			return this.type;
		}
		set {
			type = value;
		}
	}

	public ConstraintTarget Target {
		get {
			return this.target;
		}
		set {
			target = value;
		}
	}

	public int AutoTagIndex {
		get {
			return this.autoTagIndex;
		}
		set {
			autoTagIndex = value;
			fuzzyTagName = FuzzyTagDictionary.Descriptors [autoTagIndex];
		}
	}

	public string RawTags {
		get {
			return this.rawTags;
		}
		set {			
			rawTags = value;
			parsedTags = rawTags.Split (';').ToList();
		}
	}

	public List<string> ParsedTags {
		get {
			return this.parsedTags;
		}
	}

	public ConstraintAmount Amount {
		get {
			return this.amount;
		}
		set {
			amount = value;
		}
	}

	public int AbsoluteAmount {
		get {
			return this.absoluteAmount;
		}
		set {
			absoluteAmount = value;
		}
	}

	public float RelativeAmount {
		get {
			return this.relativeAmount;
		}
		set {
			relativeAmount = value;
		}
	}

	public ConstraintAmountType AmountType {
		get {
			return this.amountType;
		}
		set {
			amountType = value;
		}
	}

	public void ResetConstraint(){
		matchingChunksUsed = 0;
		matchingHallwaysUsed = 0;
		atMostAmount = -1;
	}

	public HallwayAmount HallwayAmount {
		get {
			return this.hAmount;
		}
		set {
			hAmount = value;
		}
	}

	public TagComparisonMethod TagMethod {
		get {
			return this.tagMethod;
		}
	}

	public float HallwayPriority {
		get {
			return this.hallwayPriority;
		}
		set {
			hallwayPriority = value;
		}
	}

	public void ToggleTagMethod(){
		tagMethod = tagMethod == TagComparisonMethod.And ? TagComparisonMethod.Or : TagComparisonMethod.And;
	}
}