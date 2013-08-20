﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrawlLib.SSBBTypes;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace BrawlLib.SSBB.ResourceNodes
{
    public unsafe class VIS0Node : AnimationNode
    {
        internal BRESCommonHeader* Header { get { return (BRESCommonHeader*)WorkingUncompressed.Address; } }
        internal VIS0v3* Header3 { get { return (VIS0v3*)WorkingUncompressed.Address; } }
        internal VIS0v4* Header4 { get { return (VIS0v4*)WorkingUncompressed.Address; } }
        public override ResourceType ResourceType { get { return ResourceType.VIS0; } }
        public override Type[] AllowedChildTypes
        {
            get
            {
                return new Type[] { typeof(VIS0EntryNode) };
            }
        }

        internal int _numFrames = 1, _version = 3, _loop;

        public int ConversionBias = 0;
        public int startUpVersion = 3;

        [Category("Bone Visibility Data")]
        public int Version 
        { 
            get { return _version; } 
            set 
            {
                if (_version == value)
                    return;

                if (value == startUpVersion)
                    ConversionBias = 0;
                else if (startUpVersion == 4 && value == 3)
                    ConversionBias = 1;
                else if (startUpVersion == 3 && value == 4)
                    ConversionBias = -1;

                _version = value; 
                
                SignalPropertyChange(); 
            } 
        }
        [Category("Bone Visibility Data")]
        public override int FrameCount 
        { 
            get { return _numFrames + (startUpVersion == 3 ? 1 : 0); }
            set
            {
                int bias = (startUpVersion == 3 ? 1 : 0);

                if ((_numFrames == value - bias) || (value - bias < (1 - bias)))
                    return;

                _numFrames = value - bias;

                foreach (VIS0EntryNode e in Children)
                    e.EntryCount = FrameCount;

                SignalPropertyChange();
            }
        }
        
        [Category("Bone Visibility Data")]
        public override bool Loop { get { return _loop != 0; } set { _loop = value ? 1 : 0; SignalPropertyChange(); } }

        [Category("User Data"), TypeConverter(typeof(ExpandableObjectCustomConverter))]
        public UserDataCollection UserEntries { get { return _userEntries; } set { _userEntries = value; SignalPropertyChange(); } }
        internal UserDataCollection _userEntries = new UserDataCollection();

        [Category("Bone Visibility Data")]
        public string OriginalPath { get { return _originalPath; } set { _originalPath = value; SignalPropertyChange(); } }
        public string _originalPath;

        public unsafe VIS0EntryNode CreateEntry()
        {
            VIS0EntryNode entry = new VIS0EntryNode();
            entry._entryCount = -1;
            entry.EntryCount = _numFrames + 1;
            entry.Name = this.FindName(null);
            AddChild(entry);
            return entry;
        }

        public override bool OnInitialize()
        {
            base.OnInitialize();

            _version = Header->_version;

            if (_version == 4)
            {
                VIS0v4* header = Header4;
                _numFrames = header->_numFrames;
                _loop = header->_loop;
                if ((_name == null) && (header->_stringOffset != 0))
                    _name = header->ResourceString;

                if (Header4->_origPathOffset > 0)
                    _originalPath = Header4->OrigPath;

                (_userEntries = new UserDataCollection()).Read(Header4->UserData);
            }
            else
            {
                VIS0v3* header = Header3;
                _numFrames = header->_numFrames;
                _loop = header->_loop;

                if ((_name == null) && (header->_stringOffset != 0))
                    _name = header->ResourceString;

                if (Header3->_origPathOffset > 0)
                    _originalPath = Header3->OrigPath;
            }

            return Header3->Group->_numEntries > 0;
        }

        public override int OnCalculateSize(bool force)
        {
            int size = VIS0v3.Size + 0x18 + Children.Count * 0x10;
            foreach (ResourceNode e in Children)
                size += e.CalculateSize(force);
            if (_version == 4)
            size += _userEntries.GetSize();
            return size;
        }

        public override void OnRebuild(VoidPtr address, int length, bool force)
        {
            int count = Children.Count;
            ResourceGroup* group;

            if (_version == 4)
            {
                VIS0v4* header = (VIS0v4*)address;
                *header = new VIS0v4(length, (ushort)(_numFrames - ConversionBias), (ushort)count, _loop);
                group = header->Group;
            }
            else
            {
                VIS0v3* header = (VIS0v3*)address;
                *header = new VIS0v3(length, (ushort)(_numFrames - ConversionBias), (ushort)count, _loop);
                group = header->Group;
            }

            *group = new ResourceGroup(count);
            ResourceEntry* entry = group->First;

            VoidPtr dataAddress = group->EndAddress;
            foreach (ResourceNode n in Children)
            {
                (entry++)->_dataOffset = (int)dataAddress - (int)group;

                int len = n._calcSize;
                n.Rebuild(dataAddress, len, force);
                dataAddress += len;
            }

            if (_userEntries.Count > 0 && _version == 4)
            {
                VIS0v4* header = (VIS0v4*)address;
                header->UserData = dataAddress;
                _userEntries.Write(dataAddress);
            }
        }

        public override void OnPopulate()
        {
            ResourceGroup* group = Header3->Group;
            for (int i = 0; i < group->_numEntries; i++)
                new VIS0EntryNode().Initialize(this, new DataSource((VoidPtr)group + group->First[i]._dataOffset, 0));
        }

        internal override void GetStrings(StringTable table)
        {
            table.Add(Name);
            foreach (VIS0EntryNode n in Children)
                table.Add(n.Name);

            if (_version == 4)
                foreach (UserDataClass s in _userEntries)
                    table.Add(s._name);

            if (!String.IsNullOrEmpty(_originalPath))
                table.Add(_originalPath);
        }

        protected internal override void PostProcess(VoidPtr bresAddress, VoidPtr dataAddress, int dataLength, StringTable stringTable)
        {
            base.PostProcess(bresAddress, dataAddress, dataLength, stringTable);

            VIS0v3* header = (VIS0v3*)dataAddress;

            if (_version == 4)
            {
                ((VIS0v4*)dataAddress)->ResourceStringAddress = stringTable[Name] + 4;
                if (!String.IsNullOrEmpty(_originalPath))
                    ((VIS0v4*)dataAddress)->OrigPathAddress = stringTable[_originalPath] + 4;
            }
            else
            {
                header->ResourceStringAddress = stringTable[Name] + 4;
                if (!String.IsNullOrEmpty(_originalPath))
                    header->OrigPathAddress = stringTable[_originalPath] + 4;
            }

            ResourceGroup* group = header->Group;
            group->_first = new ResourceEntry(0xFFFF, 0, 0, 0, 0);

            ResourceEntry* rEntry = group->First;

            int index = 1;
            foreach (VIS0EntryNode n in Children)
            {
                dataAddress = (VoidPtr)group + (rEntry++)->_dataOffset;
                ResourceEntry.Build(group, index++, dataAddress, (BRESString*)stringTable[n.Name]);
                n.PostProcess(dataAddress, stringTable);
            }

            if (_version == 4) _userEntries.PostProcess(((VIS0v4*)dataAddress)->UserData, stringTable);
        }

        internal static ResourceNode TryParse(DataSource source) { return ((VIS0v3*)source.Address)->_header._tag == VIS0v3.Tag ? new VIS0Node() : null; }
    }

    public unsafe class VIS0EntryNode : ResourceNode, IBoolArrayNode
    {
        internal VIS0Entry* Header { get { return (VIS0Entry*)WorkingUncompressed.Address; } }

        public byte[] _data = new byte[0];
        public int _entryCount;
        public VIS0Flags _flags;

        [Browsable(false)]
        public int EntryCount
        {
            get { return _entryCount; }
            set
            {
                if (_entryCount == 0)
                    return;

                _entryCount = value;
                int len = value.Align(32) / 8;

                if (_data.Length < len)
                {
                    byte[] newArr = new byte[len];
                    Array.Copy(_data, newArr, _data.Length);
                    _data = newArr;
                }
                SignalPropertyChange();
            }
        }

        [Category("VIS0 Entry")]
        public VIS0Flags Flags { get { return _flags; } set { _flags = value; SignalPropertyChange(); } }

        public override int OnCalculateSize(bool force)
        {
            if (_entryCount == 0) return 8;
            return _entryCount.Align(32) / 8 + 8;
        }

        public override void OnRebuild(VoidPtr address, int length, bool force)
        {
            VIS0Entry* header = (VIS0Entry*)address;
            *header = new VIS0Entry(_flags);

            if (_entryCount != 0)
                Marshal.Copy(_data, 0, header->Data, length - 8);
        }

        public override bool OnInitialize()
        {
            if ((_name == null) && (Header->_stringOffset != 0))
                _name = Header->ResourceString;

            _flags = Header->Flags;

            if ((_flags & VIS0Flags.Constant) == 0)
            {
                _entryCount = ((VIS0Node)_parent)._numFrames + 1;
                int numBytes = _entryCount.Align(32) / 8;

                SetSizeInternal(numBytes + 8);

                _data = new byte[numBytes];
                Marshal.Copy(Header->Data, _data, 0, numBytes);
            }
            else
            {
                _entryCount = 0;
                _data = new byte[0];
                SetSizeInternal(8);
            }

            return false;
        }

        public bool GetEntry(int index)
        {
            int i = index >> 3;
            if (i >= _data.Length) return false;
            int bit = 1 << (7 - (index & 7));
            return (_data[i] & bit) != 0;
        }
        public void SetEntry(int index, bool value)
        {
            int i = index >> 3;
            if (i >= _data.Length) return;
            int bit = 1 << (7 - (index & 7));
            int mask = ~bit;
            _data[i] = (byte)((_data[i] & mask) | (value ? bit : 0));
            SignalPropertyChange();
        }

        public void MakeConstant(bool value)
        {
            _flags = VIS0Flags.Constant | (value ? VIS0Flags.Enabled : 0);
            _entryCount = 0;
            SignalPropertyChange();
        }
        public void MakeAnimated()
        {
            bool enabled = false;

            if (_flags.HasFlag(VIS0Flags.Enabled))
                enabled = true;

            _flags = VIS0Flags.None;
            _entryCount = -1;
            EntryCount = ((VIS0Node)_parent)._numFrames + 1;

            if (enabled)
                for (int i = 0; i < _entryCount; i++)
                    SetEntry(i, true);

            SignalPropertyChange();
        }

        protected internal virtual void PostProcess(VoidPtr dataAddress, StringTable stringTable)
        {
            VIS0Entry* header = (VIS0Entry*)dataAddress;
            header->ResourceStringAddress = stringTable[Name] + 4;
        }
    }
}
