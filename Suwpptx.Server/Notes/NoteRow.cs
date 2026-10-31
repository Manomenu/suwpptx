namespace Suwpptx.Server.Notes;

internal sealed record NoteRow(Guid Id, string Text, DateTime CreatedAt)
{
    public Note ToNote() => new(Id, Text, new DateTimeOffset(CreatedAt));
}
