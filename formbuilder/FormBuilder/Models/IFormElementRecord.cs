﻿using FormBuilder.Components.FormElements.Anchor;
using FormBuilder.Components.FormElements.Back;
using FormBuilder.Components.FormElements.Button;
using FormBuilder.Components.FormElements.Checkbox;
using FormBuilder.Components.FormElements.Divider;
using FormBuilder.Components.FormElements.Image;
using FormBuilder.Components.FormElements.Input;
using FormBuilder.Components.FormElements.ListData;
using FormBuilder.Components.FormElements.Paragraph;
using FormBuilder.Components.FormElements.Password;
using FormBuilder.Components.FormElements.ReCaptcha;
using FormBuilder.Components.FormElements.Row;
using FormBuilder.Components.FormElements.StackLayout;
using FormBuilder.Components.FormElements.Title;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace FormBuilder.Models;

[JsonDerivedType(typeof(DividerLayoutRecord), typeDiscriminator: "Divider")]
[JsonDerivedType(typeof(FormAnchorRecord), typeDiscriminator: "Anchor")]
[JsonDerivedType(typeof(FormButtonRecord), typeDiscriminator: "Button")]
[JsonDerivedType(typeof(FormCheckboxRecord), typeDiscriminator: "Checkbox")]
[JsonDerivedType(typeof(FormInputFieldRecord), typeDiscriminator: "Input")]
[JsonDerivedType(typeof(FormPasswordFieldRecord), typeDiscriminator: "Password")]
[JsonDerivedType(typeof(FormStackLayoutRecord), typeDiscriminator: "StackLayout")]
[JsonDerivedType(typeof(ListDataRecord), typeDiscriminator: "ListData")]
[JsonDerivedType(typeof(ParagraphRecord), typeDiscriminator: "Paragraph")]
[JsonDerivedType(typeof(TitleRecord), typeDiscriminator: "Title")]
[JsonDerivedType(typeof(ImageRecord), typeDiscriminator: "Image")]
[JsonDerivedType(typeof(BackButtonRecord), typeDiscriminator: "BackButton")]
[JsonDerivedType(typeof(RowLayoutRecord), typeDiscriminator: "RowLayout")]
[JsonDerivedType(typeof(ReCaptchaRecord), typeDiscriminator: "ReCaptchaRecord")]
public interface IFormElementRecord
{
    string Id { get; set; }
    string CorrelationId { get; set; }
    string Type { get; }
    List<LabelTranslation> Labels { get; set; }
    Dictionary<string, object> HtmlAttributes { get; set; }
    string CssStyle { get; set; }
    void ExtractJson(JsonObject json);
    IFormElementRecord GetChild(string id);
    IFormElementRecord GetChildByCorrelationId(string correlationId);
    void Apply(JsonNode node);
}