using System.Runtime.CompilerServices;

// Lets the domain tests exercise defensive guards that the public API cannot reach on its own.
[assembly: InternalsVisibleTo("WWSHA.Domain.Tests")]
