export const BlockType = {
    Paragraph: "Paragraph",
    Heading1: "Heading1",
    Heading2: "Heading2",
    Heading3: "Heading3",
    BulletList: "BulletList",
    OrderedList: "OrderedList",
    TaskItem: "TaskItem",
    Image: "Image",
    File: "File",
    Video: "Video",
    CodeBlock: "CodeBlock",
    Divider: "Divider",
    Quote: "Quote"
} as const;

export type BlockType = typeof BlockType[keyof typeof BlockType];
