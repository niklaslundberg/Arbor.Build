using System.Collections.Generic;
using System.Collections.Immutable;
using Arbor.Defensive.Collections;

namespace Arbor.Build.Core.IO;

public class PathLookupSpecification(
    IEnumerable<string>? ignoredDirectorySegments = null,
    IEnumerable<string>? ignoredFileStartsWithPatterns = null,
    IEnumerable<string>? ignoredDirectorySegmentParts = null,
    IEnumerable<string>? ignoredDirectoryStartsWithPatterns = null,
    IEnumerable<string>? ignoredFileNameParts = null)
{
    public ImmutableArray<string> IgnoredFileStartsWithPatterns { get; } = ignoredFileStartsWithPatterns.SafeToReadOnlyCollection();

    public ImmutableArray<string> IgnoredDirectoryStartsWithPatterns { get; } = ignoredDirectoryStartsWithPatterns.SafeToReadOnlyCollection();

    public ImmutableArray<string> IgnoredDirectorySegments { get; } = ignoredDirectorySegments.SafeToReadOnlyCollection();

    public ImmutableArray<string> IgnoredDirectorySegmentParts { get; } = ignoredDirectorySegmentParts.SafeToReadOnlyCollection();

    public ImmutableArray<string> IgnoredFileNameParts { get; } = ignoredFileNameParts.SafeToReadOnlyCollection();
}