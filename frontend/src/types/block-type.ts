export const BlockType = {
    Paragraph: 0,
    Heading1: 1,
    Heading2: 2,
    Heading3: 3,
    BulletList: 4,
    OrderedList: 5,
    TaskItem: 6,
    Image: 7,
    File: 8,
    Video: 9,
    CodeBlock: 10,
    Divider: 11,
    Quote: 12
} as const;

export type BlockType = typeof BlockType[keyof typeof BlockType];
