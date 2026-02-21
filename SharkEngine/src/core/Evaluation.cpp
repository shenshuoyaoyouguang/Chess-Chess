#include "core/Evaluation.h"

namespace SharkEngine {

// 兵位置价值表（红方视角，rank 1-10, file 1-9）
// 数值单位：厘兵（1/100 兵）
const int Evaluation::PAWN_POSITION_VALUES[10][9] = {
    // file: 1    2    3    4    5    6    7    8    9
    {  50,  50,  50,  50,  50,  50,  50,  50,  50}, // rank 1 (黑方底线，红兵不可能到)
    {  50,  50,  50,  50,  50,  50,  50,  50,  50}, // rank 2
    {  50,  50,  50,  50,  50,  50,  50,  50,  50}, // rank 3
    {  50,  50,  50,  50,  50,  50,  50,  50,  50}, // rank 4
    {  50,  50,  50,  50,  50,  50,  50,  50,  50}, // rank 5 (河界)
    {  60,  70,  80,  90, 100,  90,  80,  70,  60}, // rank 6 (过河)
    {  80, 100, 120, 140, 150, 140, 120, 100,  80}, // rank 7 (兵初始位)
    { 100, 120, 140, 160, 170, 160, 140, 120, 100}, // rank 8
    { 120, 140, 160, 180, 190, 180, 160, 140, 120}, // rank 9
    { 150, 170, 190, 200, 210, 200, 190, 170, 150}  // rank 10 (黑方九宫)
};

// 马位置价值表
const int Evaluation::KNIGHT_POSITION_VALUES[10][9] = {
    {  50,  60,  70,  70,  70,  70,  70,  60,  50},
    {  60,  80,  90,  90,  90,  90,  90,  80,  60},
    {  70,  90, 100, 100, 100, 100, 100,  90,  70},
    {  70,  90, 100, 100, 100, 100, 100,  90,  70},
    {  80, 100, 110, 110, 110, 110, 110, 100,  80},
    {  80, 100, 110, 110, 110, 110, 110, 100,  80},
    {  70,  90, 100, 100, 100, 100, 100,  90,  70},
    {  70,  90, 100, 100, 100, 100, 100,  90,  70},
    {  60,  80,  90,  90,  90,  90,  90,  80,  60},
    {  50,  60,  70,  70,  70,  70,  70,  60,  50}
};

// 炮位置价值表
const int Evaluation::CANNON_POSITION_VALUES[10][9] = {
    {  50,  60,  70,  70,  70,  70,  70,  60,  50},
    {  60,  70,  80,  80,  80,  80,  80,  70,  60},
    {  70,  80,  90,  90,  90,  90,  90,  80,  70},
    {  80,  90, 100, 100, 100, 100, 100,  90,  80},
    {  90, 100, 110, 110, 110, 110, 110, 100,  90},
    {  90, 100, 110, 110, 110, 110, 110, 100,  90},
    {  80,  90, 100, 100, 100, 100, 100,  90,  80},
    {  70,  80,  90,  90,  90,  90,  90,  80,  70},
    {  60,  70,  80,  80,  80,  80,  80,  70,  60},
    {  50,  60,  70,  70,  70,  70,  70,  60,  50}
};

// 车位置价值表
const int Evaluation::ROOK_POSITION_VALUES[10][9] = {
    { 100, 110, 120, 130, 130, 130, 120, 110, 100},
    { 100, 110, 120, 130, 130, 130, 120, 110, 100},
    { 100, 110, 120, 130, 130, 130, 120, 110, 100},
    { 100, 110, 120, 130, 130, 130, 120, 110, 100},
    { 110, 120, 130, 140, 140, 140, 130, 120, 110},
    { 110, 120, 130, 140, 140, 140, 130, 120, 110},
    { 100, 110, 120, 130, 130, 130, 120, 110, 100},
    { 100, 110, 120, 130, 130, 130, 120, 110, 100},
    { 100, 110, 120, 130, 130, 130, 120, 110, 100},
    { 100, 110, 120, 130, 130, 130, 120, 110, 100}
};

int Evaluation::evaluate(const Board& board) {
    int score = materialScore(board) + positionScore(board);
    
    // 根据走棋方调整（返回红方视角）
    return (board.sideToMove() == Color::RED) ? score : -score;
}

int Evaluation::materialScore(const Board& board) {
    int redScore = 0;
    int blackScore = 0;
    
    for (int i = 0; i < 90; ++i) {
        Piece piece = board.pieceAt(Square::fromIndex(i));
        if (piece.isEmpty()) continue;
        
        int value = PIECE_VALUES[static_cast<int>(piece.type)];
        
        if (piece.color == Color::RED) {
            redScore += value;
        } else {
            blackScore += value;
        }
    }
    
    return redScore - blackScore;
}

int Evaluation::positionScore(const Board& board) {
    int redScore = 0;
    int blackScore = 0;
    
    for (int r = 1; r <= 10; ++r) {
        for (int f = 1; f <= 9; ++f) {
            Square sq(f, r);
            Piece piece = board.pieceAt(sq);
            if (piece.isEmpty()) continue;
            
            int posValue = 0;
            switch (piece.type) {
                case PieceType::PAWN:
                    posValue = pawnScore(sq, piece.color);
                    break;
                case PieceType::KNIGHT:
                    posValue = knightScore(sq, piece.color);
                    break;
                case PieceType::CANNON:
                    posValue = cannonScore(sq, piece.color);
                    break;
                case PieceType::ROOK:
                    posValue = rookScore(sq, piece.color);
                    break;
                default:
                    break;
            }
            
            if (piece.color == Color::RED) {
                redScore += posValue;
            } else {
                blackScore += posValue;
            }
        }
    }
    
    return redScore - blackScore;
}

int Evaluation::pawnScore(Square sq, Color color) {
    int rank = sq.rank - 1; // 0-indexed
    int file = sq.file - 1; // 0-indexed
    
    if (color == Color::RED) {
        // 红兵，需要翻转位置表
        rank = 9 - rank;
        return PAWN_POSITION_VALUES[rank][file];
    } else {
        return PAWN_POSITION_VALUES[rank][file];
    }
}

int Evaluation::knightScore(Square sq, Color color) {
    int rank = sq.rank - 1;
    int file = sq.file - 1;
    
    if (color == Color::RED) {
        rank = 9 - rank;
    }
    
    return KNIGHT_POSITION_VALUES[rank][file];
}

int Evaluation::cannonScore(Square sq, Color color) {
    int rank = sq.rank - 1;
    int file = sq.file - 1;
    
    if (color == Color::RED) {
        rank = 9 - rank;
    }
    
    return CANNON_POSITION_VALUES[rank][file];
}

int Evaluation::rookScore(Square sq, Color color) {
    int rank = sq.rank - 1;
    int file = sq.file - 1;
    
    if (color == Color::RED) {
        rank = 9 - rank;
    }
    
    return ROOK_POSITION_VALUES[rank][file];
}

int Evaluation::mobilityScore(const Board& board) {
    // 简化版本：计算合法走法数量差异
    auto redMoves = board.generateLegalMoves();
    
    // 临时切换到黑方
    // 注意：这里需要更复杂的实现，暂时返回 0
    return 0;
}

int Evaluation::kingSafetyScore(const Board& board) {
    // 简化版本：检查将帅安全性
    // TODO: 实现更复杂的安全评估
    return 0;
}

int Evaluation::mirrorRank(int rank) {
    return 9 - rank;
}

} // namespace SharkEngine
