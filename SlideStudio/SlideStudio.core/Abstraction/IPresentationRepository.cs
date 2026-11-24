using System;
using System.Collections.Generic;
using System.Text;

namespace SlideStudio.Core.Abstractions;

public interface IPresentationRepository
{
    Task SaveAsync(Presentation presentation, string filePath, CancellationToken cancellationToken = default);
    Task<Presentation> LoadAsync(string filePath, CancellationToken cancellationToken = default);
}