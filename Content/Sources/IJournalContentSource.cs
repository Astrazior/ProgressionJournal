using System.Collections.Generic;
using ProgressionJournal.Common.Progression;

namespace ProgressionJournal.Content.Sources;

public interface IJournalContentSource
{
	IEnumerable<JournalEntry> GetEntries();
}
