using Adan.Client.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adan.Client.Common.Networking
{
    public enum ControlCode
    {
        DoubleIAC,
        GoAhead,
        EchoOn,
        EchoOff,
        CustomProtocol,
        SubNegOff,
        NeedMore,
        NoCode,
    }

    public class ControlCodeAnalyser
    {
        private bool _has_iac = false;
        private bool _has_custom_protocol = false;
        private bool _has_sub_neg = false;
        private bool _has_will_code = false;
        private bool _has_will_not_code = false;

        public ControlCode State
        {
            get;
            private set;
        }

        public ControlCodeAnalyser()
        {
            State = ControlCode.NoCode;
            CustomProtocolCode = 0;
        }

        public int CustomProtocolCode
        {
            get;
            private set;
        }

        public bool ProcessNext(byte data)
        {
            if (_has_iac)
            {
                if (data == TelnetConstants.InterpretAsCommandCode)
                {
                    ResetIAC();
                    State = ControlCode.DoubleIAC;
                    return true;
                }
                else if (data == TelnetConstants.GoAheadCode)
                {
                    ResetIAC();
                    State = ControlCode.GoAhead;
                    return true;
                }
                else if (_has_custom_protocol)
                {
                    ResetIAC();
                    CustomProtocolCode = data;
                    State = ControlCode.CustomProtocol;
                    return true;
                }
                else if (_has_sub_neg)
                {
                    if (data == TelnetConstants.CustomProtocolCode)
                    {
                        _has_custom_protocol = true;
                        State = ControlCode.NeedMore;
                        return true;
                    }
                    ReportUnknown(data);
                    return false;
                }
                else if (data == TelnetConstants.SubNegotiationStartCode)
                {
                    _has_sub_neg = true;
                    State = ControlCode.NeedMore;
                    return true;
                }
                else if (data == TelnetConstants.SubNegotiationEndCode)
                {
                    ResetIAC();
                    State = ControlCode.SubNegOff;
                    return true;
                }
                else if (_has_will_code)
                {
                    ResetIAC();
                    if (data == TelnetConstants.EchoCode)
                    {
                        State = ControlCode.EchoOn;
                        return true;
                    }
                    ReportUnknown(data);
                    return false;
                }
                else if (_has_will_not_code)
                {
                    ResetIAC();
                    if (data == TelnetConstants.EchoCode)
                    {
                        State = ControlCode.EchoOff;
                        return true;
                    }
                    ReportUnknown(data);
                    return false;
                }
                else if (data == TelnetConstants.WillCode)
                {
                    _has_will_code = true;
                    State = ControlCode.NeedMore;
                    return true;
                }
                else if (data == TelnetConstants.WillNotCode)
                {
                    _has_will_not_code = true;
                    State = ControlCode.NeedMore;
                    return true;
                }
                else
                {
                    ReportUnknown(data);
                    return false;
                }
            }
            else if (data == TelnetConstants.InterpretAsCommandCode)
            {
                _has_iac = true;
                State = ControlCode.NeedMore;
                return true;
            }
            else
            {
                State = ControlCode.NoCode;
                return false;
            }
        }
        private void ReportUnknown(byte code)
        {
            ErrorLogger.Instance.Write(string.Format("Got unknown telnet code sequence {{{0}}}: " +
                "{{{1}}} (has_iac {{{2}}}, has_sub_neg {{{3}}}, has_custom_protocol {{{4}}}, has_will_code {{{5}}}, has_will_not_code {{{6}}}",
                code, _has_iac, _has_sub_neg, _has_custom_protocol, _has_will_code, _has_will_not_code));

            ResetIAC();
            State = ControlCode.NoCode;
        }
        private void ResetIAC()
        {
            _has_iac = false;
            _has_sub_neg = false;
            _has_custom_protocol = false;
            _has_will_code = false;
            _has_will_not_code = false;
        }
    }
}
