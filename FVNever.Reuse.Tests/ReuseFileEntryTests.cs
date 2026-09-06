using TruePath;
using TruePath.SystemIo;

namespace FVNever.Reuse.Tests;

public class ReuseFileEntryTests
{
    [Theory]
    [InlineData("SPDX-FileCopyrightText: frob", "frob")]
    [InlineData("Copyright (C) frob", "frob")]
    [InlineData("Copyright © frob", "frob")]
    [InlineData("© © © frob", "frob")]
    [InlineData("SPDX-FileCopyrightText: Copyright (C) © (C) frob", "frob")]
    [InlineData("SPDX-FileCopyrightText frob", "SPDX-FileCopyrightText frob")]
    [InlineData("Copyright ©", "")]
    public async Task NameIsParsedCorrectly(string fileContent, string holderName)
    {
        var file = Temporary.CreateTempFile();
        try
        {
            await file.WriteAllTextAsync(fileContent, TestContext.Current.CancellationToken);
            var content = await ReuseFileEntry.ReadFromFile(file);
            Assert.NotNull(content);
            var notice = content.CopyrightNotices.Single();
            Assert.Equal(holderName, notice.HolderName);
        }
        finally
        {
            file.Delete();
        }
    }

    [Theory]
    [InlineData("SPDX-FileCopyrightText frob")]
    [InlineData("Copyright")]
    public async Task InvalidCopyrightIsNotParsed(string fileContent)
    {
        var file = Temporary.CreateTempFile();
        try
        {
            await file.WriteAllTextAsync(fileContent, TestContext.Current.CancellationToken);
            var content = await ReuseFileEntry.ReadFromFile(file);
            Assert.Null(content);
        }
        finally
        {
            file.Delete();
        }
    }
}
