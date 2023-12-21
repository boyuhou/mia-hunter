import pandas as pd
import numpy as np


def ndx(df_high, df_low, df_close, period) -> pd.DataFrame:
    df_max_shift = df_high.shift(1).rolling(window=period, min_periods=1).max()
    df_min_shift = df_low.shift(1).rolling(window=period, min_periods=1).min()
    return 100 - 100 * (df_max_shift - df_close) / (df_max_shift - df_min_shift)


def get_bb(data, lookback, multiplier):
    std = data.rolling(lookback, min_periods=1).std()
    upper_bb = sma(data, lookback) + std * multiplier
    lower_bb = sma(data, lookback) - std * multiplier
    middle_bb = sma(data, lookback)
    return upper_bb, middle_bb, lower_bb


def sma(data, lookback):
    _sma = data.rolling(lookback).mean()
    return _sma


def compute_percentage_r(df_high, df_low, df_close, length):
    df_hh = df_high.rolling(length, min_periods=0).max()
    df_ll = df_low.rolling(length, min_periods=0).min()
    divisor = df_hh - df_ll
    return 100 - (df_hh - df_close) / divisor * 100


def compute_wam_rs(df_high, df_low, df_close):
    length1 = 63
    length2 = 126
    length3 = 189
    length4 = 252

    df_r1 = compute_percentage_r(df_high, df_low, df_close, length1)
    df_r2 = compute_percentage_r(df_high, df_low, df_close, length2)
    df_r3 = compute_percentage_r(df_high, df_low, df_close, length3)
    df_r4 = compute_percentage_r(df_high, df_low, df_close, length4)

    df_rs = 0.4 * df_r1 + 0.2 * (df_r2 + df_r3 + df_r4)
    return df_rs