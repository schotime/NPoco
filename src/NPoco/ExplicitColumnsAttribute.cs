/* PetaPoco v4.0.3.12 - A Tiny ORMish thing for your POCO's.
 * Copyright 2011-2012 Topten Software.  All Rights Reserved.
 * 
 * Apache License 2.0 - http://www.toptensoftware.com/petapoco/license
 * 
 * Special thanks to Rob Conery (@robconery) for original inspiration (ie:Massive) and for 
 * use of Subsonic's T4 templates, Rob Sullivan (@DataChomp) for hard core DBA advice 
 * and Adam Schroder (@schotime) for lots of suggestions, improvements and Oracle support
 */

//#define PETAPOCO_NO_DYNAMIC //in your project settings on .NET 3.5

using System;

namespace NPoco
{
	// Poco's marked [Explicit] require all column properties to be marked
	[AttributeUsage(AttributeTargets.Class)]
	public class ExplicitColumnsAttribute : Attribute
	{
	}
	// For non-explicit pocos, causes a property to be ignored

    // For explicit pocos, marks property as a column

    // For explicit pocos, marks property as a column

    // Specify the table name of a poco

    // Specific the primary key of a poco class (and optional sequence name for Oracle)

    // Results from paged request

    // Pass as parameter value to force to DBType.AnsiString

    // Used by IMapper to override table bindings for an object

    // Optionally provide an implementation of this to Database.Mapper

    // This will be merged with IMapper in the next major version

    // Database class ... this is where most of the action happens

    // Transaction object helps maintain transaction depth counts

    // Simple helper class for building SQL statments
}
