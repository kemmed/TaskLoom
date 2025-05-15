using Microsoft.AspNetCore.Http;
using System.Text;

public class TestSession : ISession
{
    private Dictionary<string, byte[]> _sessionStore = new Dictionary<string, byte[]>();

    public string Id => "TestSessionId";

    public bool IsAvailable => true;

    public IEnumerable<string> Keys => _sessionStore.Keys;

    public void Clear()
    {
        _sessionStore.Clear();
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task LoadAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public void Remove(string key)
    {
        _sessionStore.Remove(key);
    }

    public void Set(string key, byte[] value)
    {
        _sessionStore[key] = value;
    }

    public bool TryGetValue(string key, out byte[] value)
    {
        return _sessionStore.TryGetValue(key, out value);
    }

    public void SetInt32(string key, int value)
    {
        Set(key, BitConverter.GetBytes(value));
    }

    public bool TryGetInt32(string key, out int value)
    {
        if (TryGetValue(key, out byte[] valueBytes) && valueBytes.Length == sizeof(int))
        {
            value = BitConverter.ToInt32(valueBytes, 0);
            return true;
        }
        value = 0;
        return false;
    }

    public void SetString(string key, string value)
    {
        Set(key, Encoding.UTF8.GetBytes(value));
    }

    public bool TryGetString(string key, out string value)
    {
        if (TryGetValue(key, out byte[] valueBytes))
        {
            value = Encoding.UTF8.GetString(valueBytes);
            return true;
        }
        value = null;
        return false;
    }
}
