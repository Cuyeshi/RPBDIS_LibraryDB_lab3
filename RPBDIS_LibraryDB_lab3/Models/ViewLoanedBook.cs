using System;
using System.Collections.Generic;

namespace RPBDIS_LibraryDB_lab3.Models;

public partial class ViewLoanedBook
{
    public int LoanId { get; set; }

    public string Title { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public DateOnly LoanDate { get; set; }

    public DateOnly? ReturnDate { get; set; }

    public bool Returned { get; set; }

    public string? Employee { get; set; }
}
