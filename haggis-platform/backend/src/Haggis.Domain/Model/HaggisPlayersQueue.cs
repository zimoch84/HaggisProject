using Haggis.Domain.Extentions;
using Haggis.Domain.Interfaces;
using System;
using System.Collections.Generic;

public class HaggisPlayerQueue : ICloneable
{
    private LinkedList<Guid> _playerGuids;
    private LinkedListNode<Guid> _currentNode;
    public int Count => _playerGuids.Count;

    public HaggisPlayerQueue(List<IHaggisPlayer> players)
    {
        _playerGuids = new LinkedList<Guid>();
        foreach (var player in players)
        {
            _playerGuids.AddLast(player.GUID);
        }
        _currentNode = _playerGuids.First;
    }

    private HaggisPlayerQueue(LinkedList<Guid> playerGuids, LinkedListNode<Guid> currentNode)
    {
        _playerGuids = playerGuids;
        _currentNode = currentNode;
    }

    public void RotatePlayersClockwise()
    {
        if (_playerGuids.Count == 0)
        {
            throw new InvalidOperationException("The queue is empty.");
        }

        _currentNode = _currentNode.Next ?? _playerGuids.First;
    }

    public Guid GetCurrentPlayer()
    {
        if (_playerGuids.Count == 0)
        {
            throw new InvalidOperationException("The queue is empty.");
        }

        return _currentNode.Value;
    }

    public Guid GetNextPlayer()
    {
        if (_playerGuids.Count == 0)
        {
            throw new InvalidOperationException("The queue is empty.");
        }
        if (_playerGuids.Count == 1)
        {
            throw new InvalidOperationException("There is only One player left");
        }

        if (_currentNode.Next  != null)
        {
            return _currentNode.Next.Value;
        }

        return _playerGuids.First.Value;
    }

    public void AddToQueue(IHaggisPlayer player)
    {
        _playerGuids.AddLast(player.GUID);
        if (_playerGuids.Count == 1)
        {
            _currentNode = _playerGuids.First;
        }
    }

    public void RemoveFromQueue(IHaggisPlayer player)
    {

        if(_playerGuids.Count == 1) {
            throw new InvalidOperationException($"You cannot remove last player from queue") ;
        }
        var guidToRemove = player.GUID;
        var node = _playerGuids.Find(guidToRemove);
        if (node != null)
        {
            if (node == _currentNode)
            {
                _currentNode = _currentNode.Next ?? _playerGuids.First;
            }
            _playerGuids.Remove(node);
        }
    }

    public void SetCurrentPlayer(IHaggisPlayer player)
    {
        var guidToSet = player.GUID;
        var node = _playerGuids.Find(guidToSet);
        if (node != null)
        {
            _currentNode = node;
        }
    }

    public object Clone()
    {
        var copiedGuids = new LinkedList<Guid>(_playerGuids.DeepStructCopy());
        var copiedCurrentNode = copiedGuids.Find(_currentNode.Value);
        return new HaggisPlayerQueue(copiedGuids, copiedCurrentNode);
    }
}