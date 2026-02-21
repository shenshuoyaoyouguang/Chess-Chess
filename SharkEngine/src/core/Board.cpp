#include "core/Board.h"
#include "core/MoveGen.h"
#include <random>
#include <sstream>
#include <algorithm>

namespace SharkEngine {

// Zobrist 哈希表
namespace {
    std::array<std::array<std::array<uint64_t, 90>, 7>, 2> pieceKeys_;
    uint64_t sideKey_;
    bool zobristInitialized = false;
    
    void initZobrist() {
        if (zobristInitialized) return;
        
        std::mt19937_64 rng(123456789);
        
        for (int color = 0; color < 2; ++color) {
            for (int type = 1; type < 8; ++type) {
                for (int sq = 0; sq < 90; ++sq) {
                    pieceKeys_[color][type][sq] = rng();
                }
            }
        }
        sideKey_ = rng();
        zobristInitialized = true;
    }
}

Board::Board() : sideToMove_(Color::RED), halfMoveClock_(0), fullMoveNumber_(1), hash_(0) {
    initZobrist();
    reset();
}

void Board::reset() {
    board_.fill(Piece{});
    moveHistory_.clear();
    sideToMove_ = Color::RED;
    halfMoveClock_ = 0;
    fullMoveNumber_ = 1;
    hash_ = 0;
    setupInitialPosition();
}

void Board::setupInitialPosition() {
    // 黑方棋子（上方，rank 1-5）
    // 车
    setPiece(Square(1, 1), {PieceType::ROOK, Color::BLACK});
    setPiece(Square(9, 1), {PieceType::ROOK, Color::BLACK});
    // 马
    setPiece(Square(2, 1), {PieceType::KNIGHT, Color::BLACK});
    setPiece(Square(8, 1), {PieceType::KNIGHT, Color::BLACK});
    // 象
    setPiece(Square(3, 1), {PieceType::BISHOP, Color::BLACK});
    setPiece(Square(7, 1), {PieceType::BISHOP, Color::BLACK});
    // 士
    setPiece(Square(4, 1), {PieceType::ADVISOR, Color::BLACK});
    setPiece(Square(6, 1), {PieceType::ADVISOR, Color::BLACK});
    // 将
    setPiece(Square(5, 1), {PieceType::KING, Color::BLACK});
    // 炮
    setPiece(Square(2, 3), {PieceType::CANNON, Color::BLACK});
    setPiece(Square(8, 3), {PieceType::CANNON, Color::BLACK});
    // 卒
    for (int f = 1; f <= 9; f += 2) {
        setPiece(Square(f, 4), {PieceType::PAWN, Color::BLACK});
    }
    
    // 红方棋子（下方，rank 6-10）
    // 车
    setPiece(Square(1, 10), {PieceType::ROOK, Color::RED});
    setPiece(Square(9, 10), {PieceType::ROOK, Color::RED});
    // 马
    setPiece(Square(2, 10), {PieceType::KNIGHT, Color::RED});
    setPiece(Square(8, 10), {PieceType::KNIGHT, Color::RED});
    // 相
    setPiece(Square(3, 10), {PieceType::BISHOP, Color::RED});
    setPiece(Square(7, 10), {PieceType::BISHOP, Color::RED});
    // 仕
    setPiece(Square(4, 10), {PieceType::ADVISOR, Color::RED});
    setPiece(Square(6, 10), {PieceType::ADVISOR, Color::RED});
    // 帅
    setPiece(Square(5, 10), {PieceType::KING, Color::RED});
    // 炮
    setPiece(Square(2, 8), {PieceType::CANNON, Color::RED});
    setPiece(Square(8, 8), {PieceType::CANNON, Color::RED});
    // 兵
    for (int f = 1; f <= 9; f += 2) {
        setPiece(Square(f, 7), {PieceType::PAWN, Color::RED});
    }
}

Piece Board::pieceAt(Square sq) const {
    if (!sq.isValid()) return Piece{};
    return board_[sq.toIndex()];
}

void Board::setPiece(Square sq, Piece piece) {
    if (!sq.isValid()) return;
    int idx = sq.toIndex();
    board_[idx] = piece;
    if (piece.isValid()) {
        hash_ ^= pieceHashKey(piece.color, piece.type, sq);
    }
}

void Board::removePiece(Square sq) {
    Piece old = pieceAt(sq);
    if (old.isValid()) {
        hash_ ^= pieceHashKey(old.color, old.type, sq);
        board_[sq.toIndex()] = Piece{};
    }
}

bool Board::makeMove(const Move& move) {
    if (!move.isValid()) return false;
    if (!isLegalMove(move)) return false;
    
    Piece movingPiece = pieceAt(move.from);
    Piece capturedPiece = pieceAt(move.to);
    
    // 执行移动
    removePiece(move.from);
    
    if (capturedPiece.isValid()) {
        removePiece(move.to);
        halfMoveClock_ = 0;
    } else {
        halfMoveClock_++;
    }
    
    setPiece(move.to, movingPiece);
    
    // 保存历史
    Move recordedMove = move;
    recordedMove.captured = capturedPiece;
    moveHistory_.push_back(recordedMove);
    
    // 切换走棋方
    sideToMove_ = (sideToMove_ == Color::RED) ? Color::BLACK : Color::RED;
    hash_ ^= sideKey_;
    
    if (sideToMove_ == Color::RED) {
        fullMoveNumber_++;
    }
    
    return true;
}

void Board::unmakeMove(const Move& move) {
    if (moveHistory_.empty()) return;
    
    Move lastMove = moveHistory_.back();
    moveHistory_.pop_back();
    
    // 切换回原走棋方
    hash_ ^= sideKey_;
    sideToMove_ = (sideToMove_ == Color::RED) ? Color::BLACK : Color::RED;
    
    // 恢复棋子位置
    Piece movedPiece = pieceAt(lastMove.to);
    removePiece(lastMove.to);
    setPiece(lastMove.from, movedPiece);
    
    if (lastMove.captured.isValid()) {
        setPiece(lastMove.to, lastMove.captured);
    }
    
    halfMoveClock_ = lastMove.captured.isValid() ? 0 : halfMoveClock_ - 1;
    if (sideToMove_ == Color::BLACK) {
        fullMoveNumber_--;
    }
}

bool Board::isLegalMove(const Move& move) const {
    if (!move.from.isValid() || !move.to.isValid()) return false;
    
    Piece piece = pieceAt(move.from);
    if (piece.color != sideToMove_) return false;
    
    Piece target = pieceAt(move.to);
    if (target.color == sideToMove_) return false; // 不能吃自己的棋
    
    // 检查具体棋子的走法规则
    switch (piece.type) {
        case PieceType::PAWN:
            if (!isPawnMoveLegal(move.from, move.to, piece.color)) return false;
            break;
        case PieceType::KNIGHT:
            if (!isKnightMoveLegal(move.from, move.to)) return false;
            break;
        case PieceType::BISHOP:
            if (!isBishopMoveLegal(move.from, move.to, piece.color)) return false;
            break;
        case PieceType::ROOK: {
            // 车走直线
            int df = move.to.file - move.from.file;
            int dr = move.to.rank - move.from.rank;
            if (df != 0 && dr != 0) return false;
            // 检查路径
            int stepF = (df == 0) ? 0 : (df > 0 ? 1 : -1);
            int stepR = (dr == 0) ? 0 : (dr > 0 ? 1 : -1);
            int f = move.from.file + stepF;
            int r = move.from.rank + stepR;
            while (f != move.to.file || r != move.to.rank) {
                if (pieceAt(Square(f, r)).isValid()) return false;
                f += stepF;
                r += stepR;
            }
            break;
        }
        case PieceType::CANNON: {
            int df = move.to.file - move.from.file;
            int dr = move.to.rank - move.from.rank;
            if (df != 0 && dr != 0) return false;
            // 计算中间棋子数
            int stepF = (df == 0) ? 0 : (df > 0 ? 1 : -1);
            int stepR = (dr == 0) ? 0 : (dr > 0 ? 1 : -1);
            int count = 0;
            int f = move.from.file + stepF;
            int r = move.from.rank + stepR;
            while (f != move.to.file || r != move.to.rank) {
                if (pieceAt(Square(f, r)).isValid()) count++;
                f += stepF;
                r += stepR;
            }
            // 炮吃子需要翻山（中间隔一个）
            if (target.isValid()) {
                if (count != 1) return false;
            } else {
                if (count != 0) return false;
            }
            break;
        }
        case PieceType::ADVISOR:
            if (!isAdvisorMoveLegal(move.from, move.to, piece.color)) return false;
            break;
        case PieceType::KING:
            if (!isKingMoveLegal(move.from, move.to, piece.color)) return false;
            break;
        default:
            return false;
    }
    
    // 检查走棋后是否被将军
    // 创建临时棋盘进行测试
    Board tempBoard = *this;
    tempBoard.sideToMove_ = sideToMove_;
    tempBoard.board_ = board_;
    tempBoard.hash_ = hash_;
    
    // 模拟走棋
    tempBoard.board_[move.to.toIndex()] = tempBoard.board_[move.from.toIndex()];
    tempBoard.board_[move.from.toIndex()] = Piece{};
    tempBoard.sideToMove_ = (tempBoard.sideToMove_ == Color::RED) ? Color::BLACK : Color::RED;
    
    if (tempBoard.isInCheck(sideToMove_)) return false;
    
    return true;
}

bool Board::isInCheck(Color color) const {
    // 找到将/帅位置
    Square kingPos;
    for (int i = 0; i < 90; ++i) {
        Piece p = board_[i];
        if (p.type == PieceType::KING && p.color == color) {
            kingPos = Square::fromIndex(i);
            break;
        }
    }
    if (!kingPos.isValid()) return false;
    
    // 检查对方所有棋子是否能攻击到将/帅
    Color enemy = (color == Color::RED) ? Color::BLACK : Color::RED;
    
    for (int i = 0; i < 90; ++i) {
        Piece p = board_[i];
        if (p.color != enemy) continue;
        
        Square from = Square::fromIndex(i);
        Move attackMove(from, kingPos);
        
        // 简化检查：只检查攻击路径，不递归检查合法性
        switch (p.type) {
            case PieceType::ROOK: {
                int df = kingPos.file - from.file;
                int dr = kingPos.rank - from.rank;
                if (df != 0 && dr != 0) continue;
                int stepF = (df == 0) ? 0 : (df > 0 ? 1 : -1);
                int stepR = (dr == 0) ? 0 : (dr > 0 ? 1 : -1);
                int f = from.file + stepF;
                int r = from.rank + stepR;
                bool blocked = false;
                while (f != kingPos.file || r != kingPos.rank) {
                    if (pieceAt(Square(f, r)).isValid()) { blocked = true; break; }
                    f += stepF;
                    r += stepR;
                }
                if (!blocked) return true;
                break;
            }
            case PieceType::CANNON: {
                int df = kingPos.file - from.file;
                int dr = kingPos.rank - from.rank;
                if (df != 0 && dr != 0) continue;
                int stepF = (df == 0) ? 0 : (df > 0 ? 1 : -1);
                int stepR = (dr == 0) ? 0 : (dr > 0 ? 1 : -1);
                int f = from.file + stepF;
                int r = from.rank + stepR;
                int count = 0;
                while (f != kingPos.file || r != kingPos.rank) {
                    if (pieceAt(Square(f, r)).isValid()) count++;
                    f += stepF;
                    r += stepR;
                }
                if (count == 1) return true; // 炮翻山将军
                break;
            }
            case PieceType::KNIGHT: {
                int df = kingPos.file - from.file;
                int dr = kingPos.rank - from.rank;
                if (abs(df) + abs(dr) != 3 || abs(df) == 0 || abs(dr) == 0) continue;
                // 检查马腿
                int legF = from.file + (abs(df) == 2 ? df / 2 : 0);
                int legR = from.rank + (abs(dr) == 2 ? dr / 2 : 0);
                if (!pieceAt(Square(legF, legR)).isValid()) return true;
                break;
            }
            case PieceType::PAWN: {
                int df = kingPos.file - from.file;
                int dr = kingPos.rank - from.rank;
                // 黑卒向下（rank增大），红兵向上（rank减小）
                if (enemy == Color::BLACK) {
                    if (dr == 1 && abs(df) <= 1) return true; // 已过河
                    if (dr == 1 && df == 0 && from.rank <= 5) return true; // 未过河只能前进
                } else {
                    if (dr == -1 && abs(df) <= 1) return true;
                    if (dr == -1 && df == 0 && from.rank >= 6) return true;
                }
                break;
            }
            default:
                break;
        }
    }
    
    // 检查将帅对脸
    if (canCastleKingMove(kingPos, kingPos)) {
        // 寻找对方将帅
        for (int i = 0; i < 90; ++i) {
            Piece p = board_[i];
            if (p.type == PieceType::KING && p.color == enemy) {
                Square enemyKing = Square::fromIndex(i);
                if (enemyKing.file == kingPos.file) {
                    int minR = std::min(enemyKing.rank, kingPos.rank);
                    int maxR = std::max(enemyKing.rank, kingPos.rank);
                    bool blocked = false;
                    for (int r = minR + 1; r < maxR; ++r) {
                        if (pieceAt(Square(kingPos.file, r)).isValid()) {
                            blocked = true;
                            break;
                        }
                    }
                    if (!blocked) return true; // 将帅对脸
                }
                break;
            }
        }
    }
    
    return false;
}

GameResult Board::gameResult() const {
    // 检查当前走棋方是否有合法走法
    std::vector<Move> legalMoves = generateLegalMoves();
    if (legalMoves.empty()) {
        if (isInCheck(sideToMove_)) {
            return (sideToMove_ == Color::RED) ? GameResult::BLACK_WIN : GameResult::RED_WIN;
        }
        return GameResult::DRAW; // 无子可动，和棋
    }
    return GameResult::ONGOING;
}

std::vector<Move> Board::generateLegalMoves() const {
    return MoveGenerator::generateLegalMoves(*this);
}

std::string Board::toFEN() const {
    std::ostringstream fen;
    
    // 棋盘部分
    for (int r = 1; r <= 10; ++r) {
        int empty = 0;
        for (int f = 1; f <= 9; ++f) {
            Piece p = pieceAt(Square(f, r));
            if (p.isEmpty()) {
                empty++;
            } else {
                if (empty > 0) {
                    fen << empty;
                    empty = 0;
                }
                char c = ' ';
                switch (p.type) {
                    case PieceType::KING: c = (p.color == Color::RED) ? 'K' : 'k'; break;
                    case PieceType::ADVISOR: c = (p.color == Color::RED) ? 'A' : 'a'; break;
                    case PieceType::BISHOP: c = (p.color == Color::RED) ? 'B' : 'b'; break;
                    case PieceType::KNIGHT: c = (p.color == Color::RED) ? 'N' : 'n'; break;
                    case PieceType::ROOK: c = (p.color == Color::RED) ? 'R' : 'r'; break;
                    case PieceType::CANNON: c = (p.color == Color::RED) ? 'C' : 'c'; break;
                    case PieceType::PAWN: c = (p.color == Color::RED) ? 'P' : 'p'; break;
                    default: break;
                }
                if (c != ' ') fen << c;
            }
        }
        if (empty > 0) fen << empty;
        if (r < 10) fen << '/';
    }
    
    // 走棋方
    fen << ' ' << (sideToMove_ == Color::RED ? 'w' : 'b');
    
    // 回合数
    fen << ' ' << fullMoveNumber_ << ' ' << halfMoveClock_;
    
    return fen.str();
}

bool Board::fromFEN(const std::string& fen) {
    reset();
    board_.fill(Piece{});
    hash_ = 0;
    
    std::istringstream iss(fen);
    std::string boardStr, side, castling;
    
    iss >> boardStr >> side;
    
    // 解析棋盘
    int rank = 1;
    int file = 1;
    for (char c : boardStr) {
        if (c == '/') {
            rank++;
            file = 1;
        } else if (c >= '1' && c <= '9') {
            file += (c - '0');
        } else {
            PieceType type = PieceType::NONE;
            Color color = (c >= 'A' && c <= 'Z') ? Color::RED : Color::BLACK;
            char lower = (c >= 'A' && c <= 'Z') ? (c + 32) : c;
            
            switch (lower) {
                case 'k': type = PieceType::KING; break;
                case 'a': type = PieceType::ADVISOR; break;
                case 'b': type = PieceType::BISHOP; break;
                case 'n': type = PieceType::KNIGHT; break;
                case 'r': type = PieceType::ROOK; break;
                case 'c': type = PieceType::CANNON; break;
                case 'p': type = PieceType::PAWN; break;
                default: break;
            }
            
            if (type != PieceType::NONE && file <= 9) {
                setPiece(Square(file, rank), {type, color});
                file++;
            }
        }
    }
    
    sideToMove_ = (side == "w" || side == "W") ? Color::RED : Color::BLACK;
    if (sideToMove_ == Color::BLACK) {
        hash_ ^= sideKey_;
    }
    
    return true;
}

uint64_t Board::pieceHashKey(Color color, PieceType type, Square sq) {
    return pieceKeys_[static_cast<int>(color)][static_cast<int>(type)][sq.toIndex()];
}

uint64_t Board::sideHashKey() {
    return sideKey_;
}

void Board::updateHash(Square sq, Piece piece) {
    if (piece.isValid()) {
        hash_ ^= pieceHashKey(piece.color, piece.type, sq);
    }
}

bool Board::isValidSquare(Square sq) {
    return sq.file >= 1 && sq.file <= 9 && sq.rank >= 1 && sq.rank <= 10;
}

bool Board::isInPalace(Square sq, Color color) {
    if (sq.file < 4 || sq.file > 6) return false;
    if (color == Color::RED) {
        return sq.rank >= 8 && sq.rank <= 10;
    } else {
        return sq.rank >= 1 && sq.rank <= 3;
    }
}

bool Board::isAcrossRiver(Square sq, Color color) {
    if (color == Color::RED) {
        return sq.rank <= 5; // 红方过河是到对方区域（上半区）
    } else {
        return sq.rank >= 6; // 黑方过河是到对方区域（下半区）
    }
}

bool Board::isPawnMoveLegal(Square from, Square to, Color color) const {
    int df = to.file - from.file;
    int dr = to.rank - from.rank;
    
    if (color == Color::RED) {
        // 红兵向上走（rank减小）
        if (dr < -1 || dr > 0) return false;
        if (isAcrossRiver(from, color)) {
            // 过河后可以左右移动
            if (dr == 0 && abs(df) == 1) return true;
            if (dr == -1 && df == 0) return true;
        } else {
            // 未过河只能前进
            if (dr == -1 && df == 0) return true;
        }
    } else {
        // 黑卒向下走（rank增大）
        if (dr > 1 || dr < 0) return false;
        if (isAcrossRiver(from, color)) {
            if (dr == 0 && abs(df) == 1) return true;
            if (dr == 1 && df == 0) return true;
        } else {
            if (dr == 1 && df == 0) return true;
        }
    }
    return false;
}

bool Board::isKnightMoveLegal(Square from, Square to) const {
    int df = to.file - from.file;
    int dr = to.rank - from.rank;
    
    // 马走日字
    if (abs(df) + abs(dr) != 3) return false;
    if (abs(df) == 0 || abs(dr) == 0) return false;
    
    // 检查马腿
    int legF = from.file;
    int legR = from.rank;
    if (abs(df) == 2) {
        legF = from.file + df / 2;
    } else {
        legR = from.rank + dr / 2;
    }
    
    if (pieceAt(Square(legF, legR)).isValid()) return false; // 蹩马腿
    
    return true;
}

bool Board::isBishopMoveLegal(Square from, Square to, Color color) const {
    int df = to.file - from.file;
    int dr = to.rank - from.rank;
    
    // 象走田字
    if (abs(df) != 2 || abs(dr) != 2) return false;
    
    // 象不能过河
    if (color == Color::RED && to.rank <= 5) return false;
    if (color == Color::BLACK && to.rank >= 6) return false;
    
    // 检查象眼
    int eyeF = from.file + df / 2;
    int eyeR = from.rank + dr / 2;
    if (pieceAt(Square(eyeF, eyeR)).isValid()) return false; // 塞象眼
    
    return true;
}

bool Board::isAdvisorMoveLegal(Square from, Square to, Color color) const {
    int df = to.file - from.file;
    int dr = to.rank - from.rank;
    
    // 士走斜线一格
    if (abs(df) != 1 || abs(dr) != 1) return false;
    
    // 必须在九宫格内
    if (!isInPalace(to, color)) return false;
    
    return true;
}

bool Board::isKingMoveLegal(Square from, Square to, Color color) const {
    int df = to.file - from.file;
    int dr = to.rank - from.rank;
    
    // 将帅走直线一格
    if (abs(df) + abs(dr) != 1) return false;
    
    // 必须在九宫格内
    if (!isInPalace(to, color)) return false;
    
    return true;
}

bool Board::canCastleKingMove(Square from, Square to) const {
    // 检测将帅是否在同一列（用于对脸检测）
    return from.file == to.file;
}

} // namespace SharkEngine
