﻿// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using SimpleIdServer.Configuration;
using SimpleIdServer.IdServer.Domains;
using SimpleIdServer.IdServer.UI;

namespace SimpleIdServer.IdServer.Email;

public class IdServerEmailOptions : IOTPRegisterOptions
{
    [ConfigurationRecord("Smtp Port", null, order: 0)]
    public int SmtpPort { get; set; } = 587;
    [ConfigurationRecord("Smtp Host", null, order: 1)]
    public string SmtpHost { get; set; } = "smtp.gmail.com";
    [ConfigurationRecord("Email", null, order: 2)]
    public string? SmtpUserName { get; set; } = null;
    [ConfigurationRecord("Password", null, 3, null, CustomConfigurationRecordType.PASSWORD)]
    public string? SmtpPassword { get; set; } = null;
    [ConfigurationRecord("Content of the message", null, order: 5)]
    public string HttpBody { get; set; } = "the confirmation code is {0}";
    [ConfigurationRecord("Email of the sender", null, order: 6)]
    public string? FromEmail { get; set; } = null;
    [ConfigurationRecord("Enable SSL", null, order: 7)]
    public bool SmtpEnableSsl { get; set; } = true;
    [ConfigurationRecord("OTP Algorithm", null, order: 8)]
    public OTPTypes OTPType { get; set; } = OTPTypes.TOTP;
    [ConfigurationRecord("OTP Value", null, 9, null, CustomConfigurationRecordType.OTPVALUE)]
    public string OTPValue { get; set; } = "PBJ777ZITHOPF7AVR7I47VRSNQYVFFNY";
    [ConfigurationRecord("OTP Counter", null, 10, "OTPType=HOTP")]
    public int OTPCounter { get; set; } = 10;
    [ConfigurationRecord("TOTP Step", null, 11, "OTPType=TOTP")]
    public int TOTPStep { get; set; } = 30;
    [ConfigurationRecord("HOTP Window", null, 12, "OTPType=HOTP")]
    public int HOTPWindow { get; set; } = 5;
    public OTPAlgs OTPAlg => (OTPAlgs)OTPType;
}

public enum OTPTypes
{
    [ConfigurationRecordEnum("HOTP")]
    HOTP = 0,
    [ConfigurationRecordEnum("TOTP")]
    TOTP = 1
}