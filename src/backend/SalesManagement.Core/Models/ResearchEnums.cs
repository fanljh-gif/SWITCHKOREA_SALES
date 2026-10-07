namespace SalesManagement.Core.Models;

public enum ProspectStatus { Collected, Enriching, ReviewPending, DuplicateSuspected, OnHold, Excluded, Approved, Converted }
public enum ResearchRunKind { Manual, Scheduled, Excel }
public enum ResearchRunStatus { Queued, Running, Partial, Completed, Failed }
public enum ProspectPriority { High, Normal, Low }
public enum EvidenceConfidence { High, Medium, Low }
public enum DuplicateTargetType { Candidate, NetworkCompany, Customer }
public enum DuplicateDecision { Pending, Duplicate, Distinct }
public enum ProspectActivityAction { Collected, Enriched, Assigned, Reviewed, DuplicateDecided, Approved, Converted, Excluded }
public enum DuplicateMatchStrength { Strong, Suspected }
public enum ProspectReviewDecision { OnHold, Exclude, Approve, Distinct, Duplicate }
