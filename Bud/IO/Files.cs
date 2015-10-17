using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Bud.IO {
  public struct Files : IEnumerable<Timestamped<string>>, IExpandable<Files>
  {
    public static readonly Files Empty = new Files(Enumerable.Empty<Timestamped<string>>());
    private readonly IEnumerable<Timestamped<string>> files;

    public Files(IEnumerable<Timestamped<string>> files) {
      this.files = files;
    }

    public Files(IEnumerable<string> files) {
      this.files = files.Select(ToTimestampedFile);
    }

    public IEnumerator<Timestamped<string>> GetEnumerator() => files.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static Files Create(IEnumerable<string> paths)
      => new Files(paths);

    private static Timestamped<string> ToTimestampedFile(string path)
      => Timestamped.Create(path, File.GetLastWriteTime(path));

    public Files ExpandWith(Files other)
      => new Files(files.Concat(other.files));

    public Files Filter(Func<Timestamped<string>, bool> filter)
      => new Files(files.Where(filter));
  }
}